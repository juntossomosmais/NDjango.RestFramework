namespace NDjango.RestFramework.Serializer
{
    /// <summary>
    /// Identifies the CRUD operation that triggered a <see cref="ValidationContext{TPrimaryKey}"/>.
    /// Used by validation hooks to branch on intent (e.g., uniqueness checks must exclude the
    /// target entity on updates but not on creates). Prefer this over deriving intent from whether
    /// <see cref="ValidationContext{TPrimaryKey}.EntityId"/> is the default — that derivation is
    /// ambiguous for bulk updates (which don't carry a single id) and for int primary keys where
    /// <c>0</c> may be a valid id.
    /// </summary>
    public enum SerializerOperation
    {
        /// <summary>POST — creating a new entity.</summary>
        Create,

        /// <summary>PUT — fully replacing an existing entity identified by a single id.</summary>
        Update,

        /// <summary>PATCH — partially updating an existing entity identified by a single id.</summary>
        PartialUpdate,

        /// <summary>PUT ?ids=... — applying one payload to many entities; no single id is in scope.</summary>
        BulkUpdate
    }
}
