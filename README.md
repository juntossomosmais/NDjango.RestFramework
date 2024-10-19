# NDjango.RestFramework

NDjango Rest Framework makes you focus on business, not on boilerplate code. It's designed to follow the famous Django's slogan "The web framework for perfectionists with deadlines." 🤺

This is a copy of the convention established by [Django REST framework](https://github.com/encode/django-rest-framework), though translated to C# and adapted to the .NET Core framework.

## Quickstart with an example

Let's create a CRUD API for a `Customer` entity with a `CustomerDocument` child entity.

### Entity

Some characteristics of the entities:

- We should inherit from `BaseModel<TPrimaryKey>`.
  - The `TPrimaryKey` is the type of the primary key. In this case, we are using `Guid`.
- The `GetFields` method is mandatory. It informs which fields of the entity will be serialized in the API response.
  - For fields of child or parent entities, we can use `:` to indicate them to be serialized as well. In this case, it is necessary to perform the `Include` with a filter.

```csharp
public class Customer : BaseModel<Guid>
{
    public string Name { get; set; }
    public string CNPJ { get; set; }
    public int Age { get; set; }

    public ICollection<CustomerDocument> CustomerDocument { get; set; }

    public override string[] GetFields()
    {
        return new[] { "Id", "Name", "CNPJ", "Age", "CustomerDocument", "CustomerDocument:DocumentType", "CustomerDocument:Document" };
    }
}
```

### Entity Framework

Add the collection to the application's `DbContext`:

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Customer> Customer { get; set; }
}
```

### DTO

The DTO is required to inherit from `BaseDto<TPrimaryKey>`, like the entity.

```csharp
public class CustomerDto : BaseDto<Guid>
{
    public CustomerDto() { }

    public string Name { get; set; }
    public string CNPJ { get; set; }

    public ICollection<CustomerDocumentDto> CustomerDocuments { get; set; }
}
```

### Validation

A validation is not mandatory, but it is recommended to ensure that the data is correct. The validation is done using the `FluentValidation` library.

```csharp
public class CustomerDtoValidator : AbstractValidator<CustomerDto>
{
    public CustomerDtoValidator(IHttpContextAccessor context)
    {
        RuleFor(m => m.Name)
            .MinimumLength(3)
            .WithMessage("Name should have at least 3 characters");

        if (context.HttpContext.Request.Method == HttpMethods.Post)
            RuleFor(m => m.CNPJ)
                .NotEqual("567")
                .WithMessage("CNPJ cannot be 567");
    }
}
```

### Include child/parent entities

Previously, we included the `CustomerDocument` entity in the `Customer` entity. Check out the `GetFields` method in the `Customer` entity.

```csharp
public class CustomerDocumentIncludeFilter : Filter<Customer>
{
    public override IQueryable<Customer> AddFilter(IQueryable<Customer> query, HttpRequest request)
    {
        return query.Include(x => x.CustomerDocument);
    }
}
```

### Controller

The CRUD API is created by inheriting from the `BaseController` and passing the necessary parameters. Note how `AllowedFields` and `Filters` are set.

```csharp
[Route("api/[controller]")]
[ApiController]
public class CustomersController : BaseController<CustomerDto, Customer, Guid, ApplicationDbContext>
{
    public CustomersController(
        CustomerSerializer serializer,
        ApplicationDbContext dbContext,
        ILogger<Customer> logger)
        : base(
                serializer,
                dbContext,
                logger)
    {
        AllowedFields = new[] {
            nameof(Customer.Id),
            nameof(Customer.Name),
            nameof(Customer.CNPJ),
            nameof(Customer.Age),
        };

        Filters.Add(new QueryStringFilter<Customer>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Customer>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Customer, Guid>());
        Filters.Add(new CustomerDocumentIncludeFilter());
    }
}
```

## API Guide

### Sorting

In the `ListPaged` method, we use the query parameters `sort` or `sortDesc` to sort by a field. If not specified, we will always use the entity's `Id` field for ascending sorting.

### Filters

Filters are mechanisms applied whenever we try to retrieve entity data in the `GetSingle` and `ListPaged` methods.

#### QueryStringFilter

The `QueryStringFilter`, perhaps the most relevant, is a filter that matches the fields passed in the query parameters with the fields of the entity whose filter is allowed. All filters are created using the equals (`==`) operator.

#### QueryStringIdRangeFilter

The `QueryStringIdRangeFilter` goal is to filter the entities by `Id` based on all the `ids` provided in the query parameters.

#### QueryStringSearchFilter

The `QueryStringSearchFilter` is a filter that allows a `search` parameter to be provided in the query parameters to search, through a single input, in several fields of the entity, even performing `LIKE` on strings.

#### Implementing a filter

Given an `IQueryable<T>` and an `HttpRequest`, you can implement the filter as you prefer. Just inherit from the base class and add it to your controller:

```csharp
public class MyFilter : AspNetCore.RestFramework.Core.Filters.Filter<Seller>
{
    private readonly string _forbiddenName;

    public MyFilter(string forbiddenName)
    {
        _forbiddenName = forbiddenName;
    }

    public IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request)
    {
        return query.Where(m => m.Name != forbiddenName);
    }
}
```

```csharp
public class SellerController
{
    public SellerController(...)
        : base(...)
    {
        Filters.Add(new MyFilter("Example"));
    }
}
```

### Paginations

By default, the `BaseController` uses the class `PageNumberPagination`. [It behaves the same as DRF's `PageNumberPagination`](https://www.django-rest-framework.org/api-guide/pagination/#pagenumberpagination). Sample response:

```json
{
  "count": 13,
  "next": "http://localhost:8000/api/v1/Persons?page=3&page_size=5",
  "previous": "http://localhost:8000/api/v1/Persons?page=1&page_size=5",
  "results": [
    {
      "name": "Sal Paradise",
      "createdAt": "2024-10-19T19:22:12.0524797",
      "id": 6
    },
    {
      "name": "Odulor",
      "createdAt": "2024-10-19T19:22:15.4483365",
      "id": 7
    },
    {
      "name": "Iago",
      "createdAt": "2024-10-19T19:22:18.1077698",
      "id": 8
    },
    {
      "name": "Jafar",
      "createdAt": "2024-10-19T19:22:21.5425118",
      "id": 9
    },
    {
      "name": "Wig",
      "createdAt": "2024-10-19T19:22:23.9046811",
      "id": 10
    }
  ]
}
```

### Errors

The `ValidationErrors` and `UnexpectedError` might be returned in the `BaseController` in case of validation errors or other exceptions.

### Validation

Implement validators for the DTOs and configure your application with the extension `ModelStateValidationExtensions.ConfigureValidationResponseFormat` to ensure that in case of the `ModelState` being invalid, a `ValidationErrors` is returned. It might be necessary to add the `HttpContext` accessor to the services. Check the example below:

```csharp
services.AddControllers()
    // ...
    // At the end of AddControllers, add the following:
    .AddModelValidationAsyncActionFilter(options =>
    {
        options.OnlyApiController = true;
    })
    // ModelStateValidationExtensions
    .ConfigureValidationResponseFormat();
// ...
services.AddHttpContextAccessor();
```

### Serializer

`Serializer` is a mechanism used by the `BaseController`. Each controller has its own serializer. The serializer's methods can be overridden to add additional or different logic for specific entities. It works more or less similar to the [Django REST framework's serializers](https://www.django-rest-framework.org/api-guide/serializers/).

### Glossary

| Term           | Description                                                 |
|----------------|-------------------------------------------------------------|
| `TPrimaryKey`  | Type of the primary key of an entity, usually `Guid`.       |
| `TEntity`      | Type of the entity we are talking about in a generic class. |
| `TOrigin`      | In the `BaseController`, it is the same as `TEntity`.       |
| `TDestination` | Type of the DTO.                                            |
| `TContext`     | Type of the Entity Framework context.                       |

## Notice

This project is still in the early stages of development. We recommend that you do not use it in production environments and check the written tests to understand the current functionality.
