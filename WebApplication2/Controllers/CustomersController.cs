using CSharpRestFramework.Base;
using CSharpRestFramework.Filters;
using Microsoft.AspNetCore.Mvc;
using WebApplication2.Context;
using WebApplication2.CustomSerializers;
using WebApplication2.DTO;
using WebApplication2.Filters;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : BaseController<CustomerDTO, Customer, ApplicationDbContext>
    {

        public CustomersController(CustomerSerializer serializer,
                                   ApplicationDbContext dbContext) : base(serializer, dbContext)
        {
            AllowedFields = new [] { "Name", "CNPJ", "Age" };

            Filters.Add(new QueryStringFilter<ApplicationDbContext, Customer>(AllowedFields));
            Filters.Add(new DocumentFilter());

        }
    }
}
