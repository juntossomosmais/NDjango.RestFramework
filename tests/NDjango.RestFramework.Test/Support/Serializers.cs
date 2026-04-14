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

    public override Task<Customer> PostAsync(CustomerDto data)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<Customer> PatchAsync(PartialJsonObject<CustomerDto> originObject, Guid entityId)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<Customer> PutAsync(CustomerDto origin, Guid entityId)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<IList<Guid>> PutManyAsync(CustomerDto origin, IList<Guid> entityIds)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }

    public override Task<Customer> DeleteAsync(Guid entityId)
    {
        throw new OperationCanceledException("Simulated client disconnect");
    }

    public override Task<IList<Guid>> DeleteManyAsync(IList<Guid> entityIds)
    {
        throw new InvalidOperationException("Simulated infrastructure failure");
    }
}
