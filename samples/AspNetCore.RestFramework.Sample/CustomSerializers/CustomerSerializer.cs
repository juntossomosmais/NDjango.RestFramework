using System;
using AspNetCore.RestFramework.Core.Serializer;
using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.DTO;
using AspNetRestFramework.Sample.Models;

namespace AspNetRestFramework.Sample.CustomSerializers
{
    public class CustomerSerializer : Serializer<CustomerDto, Customer, Guid, ApplicationDbContext>
    {
        public CustomerSerializer(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
