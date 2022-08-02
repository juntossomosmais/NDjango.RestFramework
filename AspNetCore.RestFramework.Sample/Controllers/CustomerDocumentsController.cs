using System;
using AspNetCore.RestFramework.Core.Base;
using AspNetCore.RestFramework.Core.Serializer;
using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.DTO;
using AspNetRestFramework.Sample.Filters;
using AspNetRestFramework.Sample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNetRestFramework.Sample.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomerDocumentsController : BaseController<CustomerDocumentDto, CustomerDocument, Guid, ApplicationDbContext>
{
    public CustomerDocumentsController(Serializer<CustomerDocumentDto, CustomerDocument, Guid, ApplicationDbContext> serializer, ApplicationDbContext context, ILogger<CustomerDocument> logger) : base(serializer, context,logger)
    {
        Filters.Add(new CustomerFilter());
    }
}