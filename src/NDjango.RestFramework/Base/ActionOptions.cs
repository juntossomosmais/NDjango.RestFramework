namespace NDjango.RestFramework.Base;

public class ActionOptions
{
    public bool AllowPatch { get; set; } = true;
    public bool AllowPut { get; set; } = true;

    /// <summary>
    /// Whether the <c>DELETE ?ids=</c> bulk-delete endpoint is exposed. Default <c>false</c> —
    /// bulk delete is opt-in. The bulk path runs a single <c>ExecuteDeleteAsync</c> SQL
    /// statement and silently bypasses
    /// <see cref="Base.BaseController{TOrigin,TDestination,TPrimaryKey,TContext}.ValidateDestroyAsync"/>,
    /// any override of
    /// <see cref="Serializer.Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.DestroyAsync(TDestination, System.Threading.CancellationToken)"/>,
    /// EF Core <c>SaveChanges</c> interceptors, audit-on-delete hooks, and soft-delete logic.
    /// Set to <c>true</c> only when those seams either don't exist on this resource or carry
    /// rules the bulk path is allowed to skip.
    /// </summary>
    public bool AllowBulkDelete { get; set; } = false;
}
