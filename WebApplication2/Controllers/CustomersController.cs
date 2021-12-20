using CSharpRestFramework.Base;
using CSharpRestFramework.Filters;
using CSharpRestFramework.Serializer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication2.Context;
using WebApplication2.DTO;
using WebApplication2.Filters;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : BaseController<CustomerDTO, Customer, ApplicationDbContext>
    {
        private readonly string[] _allowedFields = new string[] { "Name", "CNPJ", "Age" };

        public CustomersController(ISerializer<CustomerDTO, Customer, ApplicationDbContext> serializer,
                                   ApplicationDbContext dbContext,
                                   IHttpContextAccessor _contextAccessor) : base(serializer)
        {
            var request = _contextAccessor.HttpContext.Request;

            var query = new FilterBuilder<ApplicationDbContext, Customer>(dbContext).DbSet;
            query = new DocumentFilter().AddFilter(query, request);
            query = new QueryStringFilter<ApplicationDbContext, Models.Customer>(_allowedFields).AddFilter(query, _contextAccessor.HttpContext.Request);
            query = new SortFilter<Customer>().Sort(query, request, _allowedFields);

            this.Query = query;
        }
    }
}