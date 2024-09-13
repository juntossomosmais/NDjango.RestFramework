using System;
using AspNetCore.RestFramework.Core.Serializer;

namespace AspNetCore.RestFramework.Core.Test.Support;

public class CustomerSerializer : Serializer<CustomerDto, Customer, Guid, AppDbContext>
{
    public CustomerSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }
}
