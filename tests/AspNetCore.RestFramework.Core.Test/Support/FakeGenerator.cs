using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Bogus.Extensions.Brazil;

namespace AspNetCore.RestFramework.Core.Test.Support;

public static class FakeDataGenerator
{
    private const int SELLERS_TO_GENERATE = 30;
    private const int CUSTOMERS_TO_GENERATE = 500;
    private const int DOCUMENTS_TO_GENERATE_MIN = 1;
    private const int DOCUMENTS_TO_GENERATE_MAX = 5;

    public static (IList<Seller>, IList<Customer>, IList<CustomerDocument>) GenerateFakeData()
    {
        var sellers = Enumerable
            .Range(0, SELLERS_TO_GENERATE)
            .Select(_ =>
            {
                var sellerFaker = new Faker<Seller>()
                    .RuleFor(m => m.Id, m => Guid.NewGuid())
                    .RuleFor(m => m.Name, m => m.Company.CompanyName());

                return sellerFaker.Generate();
            })
            .ToList();

        var customerDocuments = new List<CustomerDocument>();

        var customers = Enumerable
            .Range(0, CUSTOMERS_TO_GENERATE)
            .Select(_ =>
            {
                var customerFaker = new Faker<Customer>()
                    .RuleFor(m => m.Id, m => Guid.NewGuid())
                    .RuleFor(m => m.Name, m => m.Company.CompanyName())
                    .RuleFor(m => m.Age, m => m.Random.Number(18, 99))
                    .RuleFor(m => m.CNPJ, m => m.Company.Cnpj());

                var customer = customerFaker.Generate();

                var documentsToGenerate =
                    new Faker().Random.Number(DOCUMENTS_TO_GENERATE_MIN, DOCUMENTS_TO_GENERATE_MAX);
                customerDocuments.AddRange(
                    Enumerable.Range(0, documentsToGenerate)
                        .Select(_ =>
                        {
                            var docFaker = new Faker<CustomerDocument>()
                                .RuleFor(m => m.Id, m => Guid.NewGuid())
                                .RuleFor(m => m.CustomerId, customer.Id)
                                .RuleFor(m => m.DocumentType,
                                    m => m.Random.ArrayElement(new[] { "cpf", "cnpj", "xpto", "others" }))
                                .RuleFor(m => m.Document, (f, d) =>
                                {
                                    return d.DocumentType switch
                                    {
                                        "cpf" => f.Person.Cpf(),
                                        "cnpj" => f.Company.Cnpj(),
                                        "xpto" => $"xpto_{f.Random.Number(10000, 99999)}_{f.Random.Word()}",
                                        "others" => f.Address.Country(),
                                        _ => "invalid",
                                    };
                                });

                            return docFaker.Generate();
                        })
                        .ToList()
                );

                return customer;
            })
            .ToList();

        return (sellers, customers, customerDocuments);
    }
}

