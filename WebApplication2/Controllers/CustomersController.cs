using CSharpRestFramework.Base;
using CSharpRestFramework.Filters;
using CSharpRestFramework.Serializer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
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
            _allowedFields = new [] { "Name", "CNPJ", "Age" };

            Filters.Add(new QueryStringFilter<ApplicationDbContext, Customer>(_allowedFields));
            Filters.Add(new DocumentFilter());

        }

        //public override IQueryable<Customer> GetQuery()
        //{
        //    return Query.Where(x => x.Age > 80);
        //}
    }
}
