using AspNetCore.RestFramework.Core.Filters;
using Microsoft.AspNetCore.Http;
using System.Linq;
using AspNetRestFramework.Sample.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetRestFramework.Sample.Filters
{
    public class DocumentFilter : Filter<Customer>
    {
        public override IQueryable<Customer> AddFilter(IQueryable<Customer> query, HttpRequest request)
        {
            var queryString = request.Query.Select(x => new { x.Key, x.Value }).ToList();

            var documentType = "";

            if (queryString.Any(x => x.Key == "cpf"))
                documentType = "cpf";

            if (queryString.Any(x => x.Key == "cnpj"))
                documentType = "cnpj";

            var document = queryString.FirstOrDefault(x => x.Key == documentType)?.Value;
            if (string.IsNullOrWhiteSpace(document))
                return query;


            return query.Where(x => x.CustomerDocument.Any(x => x.DocumentType == documentType && x.Document == document.Value.ToString()));
        }
    }
}
