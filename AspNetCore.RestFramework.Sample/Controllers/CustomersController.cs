using AspNetCore.RestFramework.Core.Base;
using AspNetCore.RestFramework.Core.Filters;
using Microsoft.AspNetCore.Mvc;
using System;
using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.CustomSerializers;
using AspNetRestFramework.Sample.DTO;
using AspNetRestFramework.Sample.Filters;
using AspNetRestFramework.Sample.Models;

namespace AspNetRestFramework.Sample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : BaseController<CustomerDTO, Customer, Guid, ApplicationDbContext>
    {

        public CustomersController(CustomerSerializer serializer,
                                   ApplicationDbContext dbContext) : base(serializer, dbContext)
        {
            AllowedFields = new [] { "Name", "CNPJ", "Age" };
            Filters.Add(new QueryStringFilter<ApplicationDbContext, Customer>(AllowedFields));
            Filters.Add(new DocumentFilter());
            Filters.Add(new CustomerDocumentIncludeFilter());
        }
    }
}