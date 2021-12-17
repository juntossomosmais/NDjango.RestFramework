using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WebApplication2.Controllers;

namespace WebApplication2.Filters
{
    public class DocumentFilter : BackendFilter
    {
        public override Dictionary<string, string> FilterQuerySet<TDestination>(HttpRequest request)
        {

            Dictionary<string, string> fieldsToFilter = new Dictionary<string, string>();
            if(!request.Query.Keys.Contains("cpf"))
                return fieldsToFilter;

            fieldsToFilter.Add("Customer.CustomerDocuments.Type", "cpf");
            fieldsToFilter.Add("Customer.CustomerDocuments.Document", "1234");
            return fieldsToFilter;
        }
    }
}
