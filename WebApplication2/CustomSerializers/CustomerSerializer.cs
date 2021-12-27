﻿using CSharpRestFramework.Serializer;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public Task<(int Pages, List<Customer> Data)> ListCustom(int page, int pageSize, IQueryable<Customer> query)
        {
            query = query.Include(x => x.CustomerDocuments);
            return base.List(page,pageSize, query);
        }
    }
}
