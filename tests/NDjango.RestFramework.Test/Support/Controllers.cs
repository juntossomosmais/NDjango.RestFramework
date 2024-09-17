using System;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using NDjango.RestFramework.Serializer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NDjango.RestFramework.Test.Support;

#region Controllers

[Route("api/[controller]")]
[ApiController]
public class SellersController : BaseController<SellerDto, Seller, Guid, AppDbContext>
{
    public SellersController(
        Serializer<SellerDto, Seller, Guid, AppDbContext> serializer,
        AppDbContext dbContext,
        ILogger<Seller> logger)
        : base(
            serializer,
            dbContext,
            logger)
    {
        AllowedFields = new[]
        {
            nameof(Seller.Name)
        };

        Filters.Add(new QueryStringFilter<Seller>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Seller>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Seller, Guid>());
    }
}

[Route("api/[controller]")]
[ApiController]
public class IntAsIdEntitiesController : BaseController<IntAsIdEntityDto, IntAsIdEntity, int, AppDbContext>
{
    public IntAsIdEntitiesController(
        Serializer<IntAsIdEntityDto, IntAsIdEntity, int, AppDbContext> serializer,
        AppDbContext context,
        ILogger<IntAsIdEntity> logger)
        : base(
            serializer,
            context,
            new ActionOptions() { AllowPatch = false, AllowPut = false },
            logger)
    {
        AllowedFields = new[]
        {
            nameof(IntAsIdEntity.Id),
            nameof(IntAsIdEntity.Name),
        };

        Filters.Add(new QueryStringFilter<IntAsIdEntity>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<IntAsIdEntity>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<IntAsIdEntity, int>());
    }
}

[Route("api/[controller]")]
[ApiController]
public class CustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public CustomersController(
        CustomerSerializer serializer,
        AppDbContext dbContext,
        ILogger<Customer> logger)
        : base(
            serializer,
            dbContext,
            logger)
    {
        AllowedFields = new[]
        {
            nameof(Customer.Id),
            nameof(Customer.Name),
            nameof(Customer.CNPJ),
            nameof(Customer.Age),
        };

        Filters.Add(new QueryStringFilter<Customer>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Customer>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Customer, Guid>());
        Filters.Add(new DocumentFilter());
        Filters.Add(new CustomerDocumentIncludeFilter());
    }
}

[Route("api/[controller]")]
[ApiController]
public class CustomerDocumentsController : BaseController<CustomerDocumentDto, CustomerDocument, Guid, AppDbContext>
{
    public CustomerDocumentsController(
        Serializer<CustomerDocumentDto, CustomerDocument, Guid, AppDbContext> serializer,
        AppDbContext context, ILogger<CustomerDocument> logger) : base(serializer, context, logger)
    {
        AllowedFields = new[]
        {
            nameof(CustomerDocument.Document),
            nameof(CustomerDocument.DocumentType),
            nameof(CustomerDocument.CustomerId)
        };

        Filters.Add(new CustomerFilter());
        Filters.Add(new QueryStringFilter<CustomerDocument>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<CustomerDocument>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<CustomerDocument, Guid>());
    }
}

#endregion
