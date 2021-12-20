using Microsoft.AspNetCore.Mvc;
using WebApplication2.Context;
using WebApplication2.DTO;
using WebApplication2.Models;
using WebApplication2.Serializers;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerController : BaseController<SellerDto, Seller, ApplicationDbContext>
    {
        public SellerController(ISerializer<SellerDto, Seller, ApplicationDbContext> serializer, ApplicationDbContext dbContext) : base(serializer, dbContext, new ActionOptions { AllowPatch = false })
        {
        }
    }
}
