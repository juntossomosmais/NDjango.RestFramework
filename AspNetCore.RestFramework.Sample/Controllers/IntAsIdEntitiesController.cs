using AspNetCore.RestFramework.Core.Base;
using AspNetCore.RestFramework.Core.Filters;
using AspNetCore.RestFramework.Core.Serializer;
using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.DTO;
using AspNetRestFramework.Sample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNetRestFramework.Sample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IntAsIdEntitiesController : BaseController<IntAsIdEntityDto, IntAsIdEntity, int, ApplicationDbContext>
    {
        public IntAsIdEntitiesController(
            Serializer<IntAsIdEntityDto, IntAsIdEntity, int, ApplicationDbContext> serializer,
            ApplicationDbContext context,
            ILogger<IntAsIdEntity> logger)
            : base(
                  serializer,
                  context,
                  new ActionOptions() { AllowPatch = false, AllowPut = false },
                  logger)
        {
            AllowedFields = new[] {
                nameof(IntAsIdEntity.Id),
                nameof(IntAsIdEntity.Name),
            };

            Filters.Add(new QueryStringFilter<IntAsIdEntity>(AllowedFields));
            Filters.Add(new QueryStringSearchFilter<IntAsIdEntity>(AllowedFields));
            Filters.Add(new QueryStringIdRangeFilter<IntAsIdEntity, int>());
        }
    }
}
