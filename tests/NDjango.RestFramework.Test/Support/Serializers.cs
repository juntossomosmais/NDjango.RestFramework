using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    /// POST-only validation: CNPJ cannot be "567".
    /// This migrates the old FluentValidation rule that was HTTP-method-conditional.
    /// The non-ID overload is called by POST and PutMany.
    /// </summary>
    public override Task<CustomerDto> ValidateAsync(
        CustomerDto data,
        IDictionary<string, List<string>> errors)
    {
        if (data.CNPJ == "567")
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
    /// POST validation: normalize CNPJ, check length, reject all-zeros, check uniqueness.
    /// </summary>
    public override async Task<CustomerDto> ValidateAsync(
        CustomerDto data,
        IDictionary<string, List<string>> errors)
    {
        if (data.CNPJ != null)
        {
            var cnpj = Regex.Replace(data.CNPJ, @"\D", "");
            data.CNPJ = cnpj;

            if (cnpj.Length != 14)
                errors.GetOrAdd("CNPJ").Add("CNPJ must have 14 digits.");

            if (cnpj.Length == 14 && cnpj.All(c => c == '0'))
                errors.GetOrAdd("CNPJ").Add("CNPJ cannot be all zeros.");

            if (cnpj.Length == 14 && await _dbContext.Customer.AsNoTracking().AnyAsync(c => c.CNPJ == cnpj))
                errors.GetOrAdd("CNPJ").Add("Customer with this CNPJ already exists.");
        }

        if (string.IsNullOrWhiteSpace(data.Name))
            errors.GetOrAdd("Name").Add("Name is required.");

        return data;
    }

    /// <summary>
    /// PUT validation: normalize CNPJ, check length, reject all-zeros, check uniqueness (skip self).
    /// </summary>
    public override async Task<CustomerDto> ValidateAsync(
        CustomerDto data,
        Guid entityId,
        IDictionary<string, List<string>> errors)
    {
        if (data.CNPJ != null)
        {
            var cnpj = Regex.Replace(data.CNPJ, @"\D", "");
            data.CNPJ = cnpj;

            if (cnpj.Length != 14)
                errors.GetOrAdd("CNPJ").Add("CNPJ must have 14 digits.");

            if (cnpj.Length == 14 && cnpj.All(c => c == '0'))
                errors.GetOrAdd("CNPJ").Add("CNPJ cannot be all zeros.");

            if (cnpj.Length == 14 && await _dbContext.Customer.AsNoTracking().AnyAsync(c => c.CNPJ == cnpj && c.Id != entityId))
                errors.GetOrAdd("CNPJ").Add("Customer with this CNPJ already exists.");
        }

        if (string.IsNullOrWhiteSpace(data.Name))
            errors.GetOrAdd("Name").Add("Name is required.");

        return data;
    }

    /// <summary>
    /// PATCH validation: only validate fields that were sent.
    /// </summary>
    public override async Task<PartialJsonObject<CustomerDto>> ValidateAsync(
        PartialJsonObject<CustomerDto> partialData,
        Guid entityId,
        IDictionary<string, List<string>> errors)
    {
        if (partialData.IsSet(d => d.CNPJ))
        {
            var cnpj = partialData.Instance.CNPJ;
            if (cnpj != null)
            {
                cnpj = Regex.Replace(cnpj, @"\D", "");
                partialData.SetValue(d => d.CNPJ, cnpj);

                if (cnpj.Length != 14)
                    errors.GetOrAdd("CNPJ").Add("CNPJ must have 14 digits.");

                if (cnpj.Length == 14 && cnpj.All(c => c == '0'))
                    errors.GetOrAdd("CNPJ").Add("CNPJ cannot be all zeros.");

                if (cnpj.Length == 14 && await _dbContext.Customer.AsNoTracking().AnyAsync(c => c.CNPJ == cnpj && c.Id != entityId))
                    errors.GetOrAdd("CNPJ").Add("Customer with this CNPJ already exists.");
            }
        }

        if (partialData.IsSet(d => d.Name) && string.IsNullOrWhiteSpace(partialData.Instance.Name))
            errors.GetOrAdd("Name").Add("Name is required.");

        return partialData;
    }
}

public class ThrowingCustomerSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public ThrowingCustomerSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public override Task<Customer> CreateAsync(CustomerDto data)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<Customer> PartialUpdateAsync(PartialJsonObject<CustomerDto> originObject, Guid entityId)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<Customer> UpdateAsync(CustomerDto origin, Guid entityId)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<IList<Guid>> UpdateManyAsync(CustomerDto origin, IList<Guid> entityIds)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<Customer> DestroyAsync(Guid entityId)
    {
        throw new OperationCanceledException("Simulated client disconnect");
    }

    public override Task<IList<Guid>> DestroyManyAsync(IList<Guid> entityIds)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }
}
