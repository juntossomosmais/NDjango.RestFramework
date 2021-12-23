using CSharpRestFramework.Base;
using CSharpRestFramework.Serializer;
using Microsoft.AspNetCore.Mvc;
using System;
using WebApplication2.Context;
using WebApplication2.DTO;
using WebApplication2.Models;

namespace WebApplication2.Controllers
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
