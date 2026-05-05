using System;
using System.Collections.Generic;
using NDjango.RestFramework.Helpers;

namespace NDjango.RestFramework.Serializer
{
    /// <summary>
    /// Carries per-request operation metadata to per-field and cross-field validation hooks so
    /// they can branch on intent (create vs update vs partial update vs bulk update) and reach
    /// the target <see cref="EntityId"/> for skip-self uniqueness checks.
    /// </summary>
    /// <typeparam name="TPrimaryKey">The primary key type of the entity being validated.</typeparam>
    public sealed class ValidationContext<TPrimaryKey>
    {
        /// <summary>
        /// Backing presence-probe for <see cref="IsSet"/>. On PATCH this is the underlying
        /// <see cref="PartialJsonObject{T}"/>; on every other operation it is null and
        /// <see cref="IsSet"/> returns <c>true</c> for every name (the full DTO body was
        /// materialized — DRF semantics).
        /// </summary>
        private readonly PartialJsonObject? _presence;

        /// <summary>The operation that triggered validation.</summary>
        public SerializerOperation Operation { get; }

        /// <summary>
        /// The primary key of the target entity, when one is in scope. For
        /// <see cref="SerializerOperation.Create"/> and <see cref="SerializerOperation.BulkUpdate"/>
        /// this is the default value of <typeparamref name="TPrimaryKey"/> and must not be used
        /// as a lookup key.
        /// </summary>
        public TPrimaryKey? EntityId { get; }

        /// <summary><c>true</c> when <see cref="Operation"/> is <see cref="SerializerOperation.Create"/>.</summary>
        public bool IsCreate => Operation == SerializerOperation.Create;

        /// <summary><c>true</c> when <see cref="Operation"/> is <see cref="SerializerOperation.Update"/> (PUT on a single entity).</summary>
        public bool IsUpdate => Operation == SerializerOperation.Update;

        /// <summary><c>true</c> when <see cref="Operation"/> is <see cref="SerializerOperation.PartialUpdate"/> (PATCH).</summary>
        public bool IsPartialUpdate => Operation == SerializerOperation.PartialUpdate;

        /// <summary><c>true</c> when <see cref="Operation"/> is <see cref="SerializerOperation.BulkUpdate"/> (PUT with multiple ids).</summary>
        public bool IsBulkUpdate => Operation == SerializerOperation.BulkUpdate;

        /// <summary>Alias of <see cref="IsPartialUpdate"/>.</summary>
        public bool IsPartial => Operation == SerializerOperation.PartialUpdate;

        /// <summary>
        /// Returns <c>true</c> when <paramref name="fieldName"/> was actually present in the
        /// incoming request body. On PATCH this forwards to the underlying
        /// <see cref="PartialJsonObject{T}"/>'s presence tracking. On Create / Update / BulkUpdate
        /// every field is considered set (the full body was materialized — DRF semantics).
        /// </summary>
        /// <param name="fieldName">A top-level DTO property name. Use <c>nameof(Dto.Property)</c>.</param>
        public bool IsSet(string fieldName)
        {
            if (_presence is null)
                return true;
            return _presence.IsSet(fieldName);
        }

        /// <summary>
        /// Public constructor for non-PATCH contexts (Create / Update / BulkUpdate). PATCH
        /// contexts are constructed internally by the framework so the partial-presence probe
        /// can be wired automatically.
        /// </summary>
        public ValidationContext(SerializerOperation operation, TPrimaryKey? entityId)
            : this(operation, entityId, presence: null)
        {
        }

        internal ValidationContext(
            SerializerOperation operation,
            TPrimaryKey? entityId,
            PartialJsonObject? presence)
        {
            if ((operation == SerializerOperation.Update || operation == SerializerOperation.PartialUpdate)
                && EqualityComparer<TPrimaryKey?>.Default.Equals(entityId, default))
            {
                throw new ArgumentException(
                    $"{operation} requires a non-default entityId.", nameof(entityId));
            }

            Operation = operation;
            EntityId = entityId;
            _presence = presence;
        }
    }
}
