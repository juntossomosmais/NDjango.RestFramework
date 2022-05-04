using AspNetCore.RestFramework.Core.Base;
using AspNetCore.RestFramework.Core.Serializer;
using Microsoft.AspNetCore.Mvc;
using System;
using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.DTO;
using AspNetRestFramework.Sample.Models;

namespace AspNetRestFramework.Sample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerController : BaseController<SellerDto, Seller, Guid, ApplicationDbContext>
    {
        public SellerController(Serializer<SellerDto, Seller, ApplicationDbContext> serializer, ApplicationDbContext dbContext) : base(serializer, dbContext, new ActionOptions { AllowPatch = false })
        {
        }
    }
}
