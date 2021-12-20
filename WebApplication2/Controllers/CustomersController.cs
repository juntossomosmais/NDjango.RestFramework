using Microsoft.AspNetCore.Mvc;
using WebApplication2.Context;
using WebApplication2.DTO;
using WebApplication2.Filters;
using WebApplication2.Models;
using WebApplication2.Serializers;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : BaseController<CustomerDTO, Customer, ApplicationDbContext>
    {

        public CustomersController(ISerializer<CustomerDTO, Customer, ApplicationDbContext> serializer,
                                   ApplicationDbContext dbContext,
                                   IHttpContextAccessor _contextAccessor) : base(serializer)
        {
            var query = new FilterBuilder<ApplicationDbContext, Customer>(dbContext).DbSet;
            this.Query = new DocumentFilter().AddFilter(query, _contextAccessor.HttpContext.Request);
        }
    }
}