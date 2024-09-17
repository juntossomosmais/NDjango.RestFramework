using System;
using NDjango.RestFramework.Serializer;

namespace NDjango.RestFramework.Test.Support;

public class CustomerSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public CustomerSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }
}
