using CSharpRestFramework.Filters;
using Microsoft.AspNetCore.Http;
using System.Linq;
using WebApplication2.Models;

namespace WebApplication2.Filters
{
    public class DocumentFilter : Filter<Customer>
    {
        public override IQueryable<Customer> AddFilter(IQueryable<Customer> query, HttpRequest request)
        {
            var queryString = request.Query.Select(x => new { x.Key, x.Value}).ToList();
            
            var documentType = "";

            if (queryString.Any(x => x.Key == "cpf"))
                documentType = "cpf";

            if (queryString.Any(x => x.Key == "cnpj"))
                documentType = "cnpj";

            var document = queryString.FirstOrDefault(x => x.Key == documentType)?.Value;
            if (string.IsNullOrWhiteSpace(document))
                return query;

            return query.Where(x => x.CustomerDocuments.Any(x => x.DocumentType == documentType && x.Document == document.Value.ToString()));
        }
    }
}
