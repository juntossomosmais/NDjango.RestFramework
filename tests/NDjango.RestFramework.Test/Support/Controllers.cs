using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Filters;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Paginations;
using NDjango.RestFramework.Serializer;

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
            new ActionOptions() { AllowPatch = false, AllowPut = false, AllowDelete = false, AllowBulkDelete = false },
            logger)
    {
        AllowedFields =
        [
            nameof(IntAsIdEntity.Id),
            nameof(IntAsIdEntity.Name),
            nameof(IntAsIdEntity.CreatedAt)
        ];

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
            new ActionOptions { AllowBulkDelete = true },
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

[Route("api/[controller]")]
[ApiController]
public class ValidatingCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public ValidatingCustomersController(
        ValidatingCustomerSerializer serializer,
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
    }
}

[Route("api/[controller]")]
[ApiController]
public class ThrowingCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public ThrowingCustomersController(
        ThrowingCustomerSerializer serializer,
        AppDbContext dbContext,
        ILogger<Customer> logger)
        : base(
            serializer,
            dbContext,
            new ActionOptions { AllowBulkDelete = true },
            logger)
    {
        AllowedFields = new[]
        {
            nameof(Customer.Id),
            nameof(Customer.Name),
            nameof(Customer.CNPJ),
        };
    }

    public override IQueryable<Customer> GetQuerySet()
    {
        throw new InvalidOperationException("Simulated query failure");
    }
}

/// <summary>
/// Sibling of <see cref="ThrowingCustomersController"/> that uses the throwing serializer
/// but inherits the default <see cref="BaseController{TOrigin,TDestination,TPrimaryKey,TContext}.GetQuerySet"/>.
/// Lets the exception-propagation tests for write actions assert that serializer-thrown
/// exceptions still bubble even though the controller now resolves a row-scoped queryset
/// before invoking the serializer.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ThrowingSerializerOnlyCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public ThrowingSerializerOnlyCustomersController(
        ThrowingCustomerSerializer serializer,
        AppDbContext dbContext,
        ILogger<Customer> logger)
        : base(
            serializer,
            dbContext,
            new ActionOptions { AllowBulkDelete = true },
            logger)
    {
        AllowedFields = new[]
        {
            nameof(Customer.Id),
            nameof(Customer.Name),
            nameof(Customer.CNPJ),
        };
    }
}

[Route("api/[controller]")]
[ApiController]
public class PerFieldCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public PerFieldCustomersController(
        PerFieldCustomerSerializer serializer,
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
    }
}

[Route("api/[controller]")]
[ApiController]
public class PerFieldCrossFieldCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public PerFieldCrossFieldCustomersController(
        PerFieldWithCrossFieldSerializer serializer,
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
    }
}

[Route("api/[controller]")]
[ApiController]
public class PerFieldShortCircuitCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public PerFieldShortCircuitCustomersController(
        PerFieldShortCircuitSerializer serializer,
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
    }
}

[Route("api/[controller]")]
[ApiController]
public class ContextCapturingCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public ContextCapturingCustomersController(
        ContextCapturingSerializer serializer,
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
    }
}

[Route("api/[controller]")]
[ApiController]
public class MisnamedHookCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public MisnamedHookCustomersController(
        MisnamedHookSerializer serializer,
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
        };
    }
}

/// <summary>
/// Controller for exercising the <c>ValidateDestroyAsync</c> hook end-to-end. The hook
/// short-circuits the delete with a non-field error whenever the loaded customer's
/// <c>Name</c> is "BLOCKED", and records the entity it received plus a call counter so
/// tests can probe the seam without reaching into private state.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ValidateDestroyCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    private readonly ValidateDestroyCustomerSerializer _spy;

    public ValidateDestroyCustomersController(
        ValidateDestroyCustomerSerializer serializer,
        AppDbContext dbContext,
        ILogger<Customer> logger)
        : base(serializer, dbContext, logger)
    {
        _spy = serializer;
        AllowedFields = new[]
        {
            nameof(Customer.Id),
            nameof(Customer.Name),
            nameof(Customer.CNPJ),
            nameof(Customer.Age),
        };
    }

    protected override Task ValidateDestroyAsync(
        Customer instance,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken)
    {
        _spy.RecordValidateDestroyCall(instance);

        if (instance.Name == "BLOCKED")
            errors.GetOrAdd(ValidationErrors.NonFieldErrorsKey).Add("Customer is blocked from deletion.");

        return Task.CompletedTask;
    }
}

[Route("api/[controller]")]
[ApiController]
public class InvalidFieldEntitiesController : BaseController<InvalidFieldEntityDto, InvalidFieldEntity, Guid, AppDbContext>
{
    public InvalidFieldEntitiesController(
        Serializer<InvalidFieldEntityDto, InvalidFieldEntity, Guid, AppDbContext> serializer,
        AppDbContext dbContext,
        ILogger<InvalidFieldEntity> logger)
        : base(serializer, dbContext, logger)
    {
    }
}

/// <summary>
/// Controller that overrides every <c>Perform*Async</c> seam to (1) record call counters
/// on a singleton spy and (2) post-process the persisted entity by appending a marker to
/// <see cref="Customer.Name"/>. Together these prove the seams fire from the action methods
/// and that overriding them changes observable behavior.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PerformHookCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    private readonly PerformHookSpy _spy;
    private readonly AppDbContext _dbContext;

    public PerformHookCustomersController(
        Serializer<CustomerDto, Customer, Guid, AppDbContext> serializer,
        AppDbContext dbContext,
        PerformHookSpy spy,
        ILogger<Customer> logger)
        : base(serializer, dbContext, logger)
    {
        _spy = spy;
        _dbContext = dbContext;
        AllowedFields = new[]
        {
            nameof(Customer.Id),
            nameof(Customer.Name),
            nameof(Customer.CNPJ),
            nameof(Customer.Age),
        };
    }

    protected override async Task<Customer> PerformCreateAsync(
        CustomerDto data,
        CancellationToken cancellationToken)
    {
        _spy.IncrementPerformCreate();
        var created = await base.PerformCreateAsync(data, cancellationToken);
        created.Name += "_perform_created";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    protected override async Task<Customer> PerformUpdateAsync(
        Customer instance,
        CustomerDto data,
        CancellationToken cancellationToken)
    {
        _spy.IncrementPerformUpdate();
        var updated = await base.PerformUpdateAsync(instance, data, cancellationToken);
        updated.Name += "_perform_updated";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return updated;
    }

    protected override async Task<Customer> PerformPartialUpdateAsync(
        Customer instance,
        PartialJsonObject<CustomerDto> data,
        CancellationToken cancellationToken)
    {
        _spy.IncrementPerformPartialUpdate();
        var updated = await base.PerformPartialUpdateAsync(instance, data, cancellationToken);
        updated.Name += "_perform_patched";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return updated;
    }

    protected override Task PerformDestroyAsync(
        Customer instance,
        CancellationToken cancellationToken)
    {
        _spy.IncrementPerformDestroy();
        _spy.RecordPerformDestroyInstanceName(instance.Name);
        return base.PerformDestroyAsync(instance, cancellationToken);
    }
}

/// <summary>
/// Singleton call-counter for <see cref="PerformHookCustomersController"/>. Resolved by
/// integration tests via <c>Services.GetRequiredService&lt;PerformHookSpy&gt;()</c>.
/// Counters are atomic so concurrent overrides cannot under-count.
/// </summary>
public class PerformHookSpy
{
    private int _performCreateCalls;
    private int _performUpdateCalls;
    private int _performPartialUpdateCalls;
    private int _performDestroyCalls;
    private string? _lastDestroyedInstanceName;

    public int PerformCreateCalls => Volatile.Read(ref _performCreateCalls);
    public int PerformUpdateCalls => Volatile.Read(ref _performUpdateCalls);
    public int PerformPartialUpdateCalls => Volatile.Read(ref _performPartialUpdateCalls);
    public int PerformDestroyCalls => Volatile.Read(ref _performDestroyCalls);
    public string? LastDestroyedInstanceName => Volatile.Read(ref _lastDestroyedInstanceName);

    public void IncrementPerformCreate() => Interlocked.Increment(ref _performCreateCalls);
    public void IncrementPerformUpdate() => Interlocked.Increment(ref _performUpdateCalls);
    public void IncrementPerformPartialUpdate() => Interlocked.Increment(ref _performPartialUpdateCalls);
    public void IncrementPerformDestroy() => Interlocked.Increment(ref _performDestroyCalls);

    public void RecordPerformDestroyInstanceName(string? name)
        => Interlocked.Exchange(ref _lastDestroyedInstanceName, name);

    public void Reset()
    {
        Interlocked.Exchange(ref _performCreateCalls, 0);
        Interlocked.Exchange(ref _performUpdateCalls, 0);
        Interlocked.Exchange(ref _performPartialUpdateCalls, 0);
        Interlocked.Exchange(ref _performDestroyCalls, 0);
        Interlocked.Exchange(ref _lastDestroyedInstanceName, null);
    }
}

[Route("api/[controller]")]
[ApiController]
public class InvalidAllowedFieldEntitiesController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public InvalidAllowedFieldEntitiesController(
        CustomerSerializer serializer,
        AppDbContext dbContext,
        ILogger<Customer> logger)
        : base(serializer, dbContext, logger)
    {
        AllowedFields = new[] { "Name", "NonExistentAllowedField" };
    }
}

/// <summary>
/// Drives the cross-tenant write security tests. The <see cref="TenantFilter"/> reads
/// an <c>X-Tenant</c> header and scopes the queryset by <see cref="Customer.Region"/>;
/// because the controller now threads <c>FilterQuery(...)</c> into every action's
/// load step, PUT/PATCH/DELETE/DELETE-many requests for a row in another tenant return
/// 404 instead of mutating it.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class TenantScopedCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public TenantScopedCustomersController(
        Serializer<CustomerDto, Customer, Guid, AppDbContext> serializer,
        AppDbContext dbContext,
        ILogger<Customer> logger)
        : base(
            serializer,
            dbContext,
            new ActionOptions { AllowBulkDelete = true },
            logger)
    {
        AllowedFields = new[]
        {
            nameof(Customer.Id),
            nameof(Customer.Name),
            nameof(Customer.CNPJ),
            nameof(Customer.Age),
            nameof(Customer.Region),
        };

        Filters.Add(new TenantFilter());
        Filters.Add(new QueryStringFilter<Customer>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Customer, Guid>());
    }
}

/// <summary>
/// Pins the documented instance-aware override pattern: now that filter-scoping happens
/// at the controller's load step (and the <c>Perform*</c> hooks receive the loaded
/// instance directly, queryset-free), a hook override is the canonical place to enforce
/// extra instance-shaped predicates. This controller refuses the update with 403 when
/// the request's <c>X-Region</c> header does not match <see cref="Customer.Region"/>
/// on the already-loaded instance.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class RegionGuardedCustomersController : BaseController<CustomerDto, Customer, Guid, AppDbContext>
{
    public RegionGuardedCustomersController(
        Serializer<CustomerDto, Customer, Guid, AppDbContext> serializer,
        AppDbContext dbContext,
        ILogger<Customer> logger)
        : base(serializer, dbContext, logger)
    {
        AllowedFields = new[]
        {
            nameof(Customer.Id),
            nameof(Customer.Name),
            nameof(Customer.CNPJ),
            nameof(Customer.Age),
            nameof(Customer.Region),
        };
    }

    protected override async Task<Customer> PerformUpdateAsync(
        Customer instance,
        CustomerDto data,
        CancellationToken cancellationToken)
    {
        // Instance-shaped guard: the loaded entity is the source of truth — not the DTO.
        // The override fires only after filter-scoped load, so the row is one the caller
        // could read; this layer adds a header-vs-state predicate that can't be expressed
        // as a row-scoping Filter.
        var headerRegion = HttpContext.Request.Headers.TryGetValue("X-Region", out var values)
            ? values.ToString()
            : null;
        if (!string.Equals(headerRegion, instance.Region, StringComparison.Ordinal))
            throw new RegionMismatchException(instance.Region, headerRegion);

        return await base.PerformUpdateAsync(instance, data, cancellationToken);
    }
}

/// <summary>
/// Sentinel thrown by <see cref="RegionGuardedCustomersController"/>'s override-path test
/// to prove the hook executed and inspected the loaded instance. The integration test
/// asserts the exception propagates out of the middleware pipeline so it can introspect
/// the failure shape directly.
/// </summary>
public class RegionMismatchException : Exception
{
    public string? InstanceRegion { get; }
    public string? HeaderRegion { get; }

    public RegionMismatchException(string? instanceRegion, string? headerRegion)
        : base($"Region mismatch: instance='{instanceRegion}' header='{headerRegion}'.")
    {
        InstanceRegion = instanceRegion;
        HeaderRegion = headerRegion;
    }
}

#endregion
