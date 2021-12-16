using Microsoft.AspNetCore.Mvc;
using WebApplication2.Context;
using WebApplication2.DTO;
using WebApplication2.Filters;
using WebApplication2.Models;
using WebApplication2.Serializers;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : BaseController<CustomerDTO, Customer, ApplicationDbContext>
    {
        public CustomersController(ISerializer<CustomerDTO, Customer, ApplicationDbContext> serializer) : base(serializer)
        {
            this.FilterFields.Add("Name");
            this.Filters.Add(() => new QueryStringFilter().Filter<Customer>(HttpContext.Request, FilterFields));
            //this.Filters.Add(() => new FilterCustomerName())
        }
    }
}
