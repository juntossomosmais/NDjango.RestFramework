using AspNetCore.RestFramework.Core.Base;
using AspNetCore.RestFramework.Core.Serializer;
using Microsoft.AspNetCore.Mvc;
using System;
using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.DTO;
using AspNetRestFramework.Sample.Models;
using Microsoft.Extensions.Logging;
using AspNetCore.RestFramework.Core.Filters;

namespace AspNetRestFramework.Sample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellersController : BaseController<SellerDto, Seller, Guid, ApplicationDbContext>
    {
        public SellersController(
            Serializer<SellerDto, Seller, Guid, ApplicationDbContext> serializer,
            ApplicationDbContext dbContext,
            ILogger<Seller> logger)
            : base(
                  serializer,
                  dbContext,
                  logger)
        {
            AllowedFields = new[] {
                nameof(Seller.Name)
            };

            Filters.Add(new QueryStringFilter<Seller>(AllowedFields));
            Filters.Add(new QueryStringSearchFilter<Seller>(AllowedFields));
            Filters.Add(new QueryStringIdRangeFilter<Seller, Guid>());
        }
    }
}
