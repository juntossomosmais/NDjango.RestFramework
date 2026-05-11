using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

/// <summary>
/// Append-only ledger: only GET and POST are allowed. Every mutating action is disabled via
/// <see cref="ActionOptions"/>; disabled hits return <c>405 Method Not Allowed</c> while
/// staying listed in OpenAPI / OPTIONS (documented but off by default — the library's
/// 405-inline contract, not <c>[NonAction]</c>).
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuditLogsController : BaseController<AuditLogDto, AuditLog, int, AppDbContext>
{
    public AuditLogsController(
        AuditLogSerializer serializer,
        AppDbContext db,
        ILogger<AuditLog> logger)
        : base(
            serializer,
            db,
            new ActionOptions
            {
                AllowPatch = false,
                AllowPut = false,
                AllowDelete = false,
                AllowBulkDelete = false,
            },
            logger)
    {
        AllowedFields = new[]
        {
            nameof(AuditLog.Id),
            nameof(AuditLog.Action),
            nameof(AuditLog.EntityName),
        };
        Filters.Add(new QueryStringFilter<AuditLog>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<AuditLog>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<AuditLog, int>());
    }
}
