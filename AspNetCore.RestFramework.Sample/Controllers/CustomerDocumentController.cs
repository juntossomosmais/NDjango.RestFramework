using System;
using AspNetCore.RestFramework.Core.Base;
using AspNetCore.RestFramework.Core.Serializer;
using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.DTO;
using AspNetRestFramework.Sample.Filters;
using AspNetRestFramework.Sample.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetRestFramework.Sample.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomerDocumentController : BaseController<CustomerDocumentsDTO, CustomerDocument, Guid, ApplicationDbContext>
{
    public CustomerDocumentController(Serializer<CustomerDocumentsDTO, CustomerDocument, Guid, ApplicationDbContext> serializer, ApplicationDbContext context) : base(serializer, context)
    {
        Filters.Add(new CustomerFilter());
    }
}