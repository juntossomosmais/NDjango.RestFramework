using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using WebApplication2.Controllers;

namespace WebApplication2.Filters
{
    public class QueryStringFilter : BackendFilter
    {
        public override Dictionary<string, string> Filter<TDestination>(HttpRequest httpRequest, List<string> allowedFields)
        {
            Dictionary<string, string> fieldsToFilter = new Dictionary<string, string>();

            foreach (var item in httpRequest.Query)
                if (allowedFields.Contains(item.Key))
                    fieldsToFilter.Add(item.Key, item.Value);

            return fieldsToFilter;

        }
    }
}
