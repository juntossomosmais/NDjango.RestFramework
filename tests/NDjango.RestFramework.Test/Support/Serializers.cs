using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Serializer;

namespace NDjango.RestFramework.Test.Support;

public class CustomerSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public CustomerSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    /// <summary>
    /// Cross-field rule: CNPJ cannot be "567" on POST or PutMany.
    /// </summary>
    public override Task<CustomerDto> ValidateAsync(
        CustomerDto data,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        if ((context.IsCreate || context.IsBulkUpdate) && data.CNPJ == "567")
            errors.GetOrAdd("CNPJ").Add("CNPJ cannot be 567");

        return Task.FromResult(data);
    }
}

public class ValidatingCustomerSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public ValidatingCustomerSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    /// <summary>
    /// Per-field hook for CNPJ: normalize, check length, reject all-zeros, check uniqueness
    /// (skip-self when updating). The returned value replaces the DTO's CNPJ — DRF semantics.
    /// </summary>
    public async Task<string> ValidateCNPJAsync(
        string value,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        if (value == null)
            return value!;

        var cnpj = Regex.Replace(value, @"\D", "");

        if (cnpj.Length != 14)
            errors.GetOrAdd("CNPJ").Add("CNPJ must have 14 digits.");

        if (cnpj.Length == 14 && cnpj.All(c => c == '0'))
            errors.GetOrAdd("CNPJ").Add("CNPJ cannot be all zeros.");

        if (cnpj.Length == 14)
        {
            var query = _dbContext.Customer.AsNoTracking().Where(c => c.CNPJ == cnpj);
            if (!context.IsCreate && !context.IsBulkUpdate)
                query = query.Where(c => c.Id != context.EntityId);

            if (await query.AnyAsync(cancellationToken))
                errors.GetOrAdd("CNPJ").Add("Customer with this CNPJ already exists.");
        }

        return cnpj;
    }

    /// <summary>
    /// Per-field hook for Name: required (only when sent — PATCH skip is automatic).
    /// </summary>
    public Task<string> ValidateNameAsync(
        string value,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.GetOrAdd("Name").Add("Name is required.");

        return Task.FromResult(value);
    }
}

/// <summary>
/// Serializer that uses per-field Validate{Property}Async hooks to normalize CNPJ
/// and validate Name, eliminating the triple-overload duplication.
/// </summary>
public class PerFieldCustomerSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public PerFieldCustomerSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task<string> ValidateCNPJAsync(
        string value,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        if (value != null)
        {
            var cnpj = Regex.Replace(value, @"\D", "");

            if (cnpj.Length != 14)
                errors.GetOrAdd("CNPJ").Add("CNPJ must have 14 digits.");

            if (cnpj.Length == 14 && cnpj.All(c => c == '0'))
                errors.GetOrAdd("CNPJ").Add("CNPJ cannot be all zeros.");

            if (cnpj.Length == 14)
            {
                var query = _dbContext.Customer.AsNoTracking().Where(c => c.CNPJ == cnpj);
                if (!context.IsCreate)
                    query = query.Where(c => c.Id != context.EntityId);

                if (await query.AnyAsync(cancellationToken))
                    errors.GetOrAdd("CNPJ").Add("Customer with this CNPJ already exists.");
            }

            return cnpj;
        }

        return value;
    }

    public Task<string> ValidateNameAsync(
        string value,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.GetOrAdd("Name").Add("Name is required.");

        return Task.FromResult(value);
    }
}

/// <summary>
/// Serializer that uses per-field hooks AND a cross-field ValidateAsync(data, context, errors) override
/// to test the full pipeline ordering.
/// </summary>
public class PerFieldWithCrossFieldSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public bool CrossFieldCalled { get; private set; }

    public PerFieldWithCrossFieldSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public Task<string> ValidateCNPJAsync(
        string value,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        if (value != null)
        {
            var cnpj = Regex.Replace(value, @"\D", "");
            return Task.FromResult(cnpj);
        }

        return Task.FromResult(value);
    }

    public override Task<CustomerDto> ValidateAsync(
        CustomerDto data,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        CrossFieldCalled = true;

        // Cross-field rule: Name and CNPJ cannot be equal
        if (data.Name != null && data.CNPJ != null && data.Name == data.CNPJ)
            errors.GetOrAdd("Name").Add("Name cannot be the same as CNPJ.");

        return Task.FromResult(data);
    }
}

/// <summary>
/// Serializer with per-field hooks that always add errors, used to verify
/// that cross-field ValidateAsync is NOT called when per-field hooks fail.
/// </summary>
public class PerFieldShortCircuitSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public bool CrossFieldCalled { get; private set; }

    public PerFieldShortCircuitSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public Task<string> ValidateCNPJAsync(
        string value,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        errors.GetOrAdd("CNPJ").Add("Always fails.");
        return Task.FromResult(value);
    }

    public override Task<CustomerDto> ValidateAsync(
        CustomerDto data,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        CrossFieldCalled = true;
        return Task.FromResult(data);
    }
}

/// <summary>
/// Serializer that records the <see cref="ValidationContext{TPrimaryKey}"/> its per-field hook
/// received, so integration tests can assert the controller signaled the right
/// <see cref="SerializerOperation"/> (e.g., BulkUpdate for PutMany, not Create).
/// </summary>
public class ContextCapturingSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public ValidationContext<Guid> LastContext { get; private set; }

    public ContextCapturingSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public Task<string> ValidateNameAsync(
        string value,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        LastContext = context;
        return Task.FromResult(value);
    }
}

/// <summary>
/// Serializer with a misnamed hook: ValidateCnjAsync instead of ValidateCNPJAsync.
/// Used for startup validation testing.
/// </summary>
public class MisnamedHookSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public MisnamedHookSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public Task<string> ValidateCnjAsync(
        string value,
        ValidationContext<Guid> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(value);
    }
}

public class ThrowingCustomerSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public ThrowingCustomerSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public override Task<Customer> CreateAsync(CustomerDto data, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<Customer?> PartialUpdateAsync(
        PartialJsonObject<CustomerDto> originObject,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<Customer?> UpdateAsync(
        CustomerDto origin,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<IList<Guid>> UpdateManyAsync(
        CustomerDto origin,
        IList<Guid> entityIds,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<Customer?> DestroyAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        throw new OperationCanceledException("Simulated client disconnect");
    }

    public override Task<IList<Guid>> DestroyManyAsync(
        IList<Guid> entityIds,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }
}
