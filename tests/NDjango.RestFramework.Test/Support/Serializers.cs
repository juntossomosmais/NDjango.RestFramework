using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Serializer;

namespace NDjango.RestFramework.Test.Support;

public class CustomerSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public CustomerSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
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
