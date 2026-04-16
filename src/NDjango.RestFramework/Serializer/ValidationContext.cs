using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Legacy alias for <see cref="IsPartialUpdate"/>. Kept for source compatibility with
        /// call sites written against the initial per-field validation preview.
        /// </summary>
        public bool IsPartial => Operation == SerializerOperation.PartialUpdate;

        /// <summary>
        /// Constructs a validation context for the given <paramref name="operation"/> and optional
        /// <paramref name="entityId"/>. Throws <see cref="ArgumentException"/> if
        /// <paramref name="operation"/> requires a concrete entity id (Update / PartialUpdate) but
        /// the default value of <typeparamref name="TPrimaryKey"/> was supplied.
        /// </summary>
        public ValidationContext(SerializerOperation operation, TPrimaryKey? entityId)
        {
            if ((operation == SerializerOperation.Update || operation == SerializerOperation.PartialUpdate)
                && EqualityComparer<TPrimaryKey?>.Default.Equals(entityId, default))
            {
                throw new ArgumentException(
                    $"{operation} requires a non-default entityId.", nameof(entityId));
            }

            Operation = operation;
            EntityId = entityId;
        }
    }
}
