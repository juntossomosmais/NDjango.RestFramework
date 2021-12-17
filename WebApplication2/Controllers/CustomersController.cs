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
            FilterFields.Add("Name");
            FilterFields.Add("CNPJ");
            Filters.Add(() => new QueryStringFilter().FilterQuerySet<Customer>(HttpContext.Request, FilterFields));
            Filters.Add(() => new DocumentFilter().FilterQuerySet<Customer>(HttpContext.Request));
        }
    }
}