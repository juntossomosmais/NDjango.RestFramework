using CSharpRestFramework.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication2.Context;
using WebApplication2.DTO;
using WebApplication2.Models;

namespace WebApplication2.CustomSerializers
{
    public class CustomerSerializer : Serializer<CustomerDTO, Customer, ApplicationDbContext>
    {
        public CustomerSerializer(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }

        public override IEnumerable<string> Validate(CustomerDTO data, OperationType operation)
        {
            var errors = new List<string>();

            if (operation == OperationType.Create)
            {
                if (data.CNPJ == "567")
                    errors.Add("CNPJ cannot be 567");
            }

            else if (operation == OperationType.Update)
                errors.AddRange(new List<string>());

            errors.AddRange(base.Validate(data, operation));

            return errors;
        }
    }
}
