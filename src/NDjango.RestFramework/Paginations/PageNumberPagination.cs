using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace NDjango.RestFramework.Paginations;

public class PageNumberPagination<TDestination> : Pagination<TDestination>
{
    private readonly string _pageSizeQueryParam;
    private readonly string _pageNumberQueryParam;
    private string _url;

    public PageNumberPagination(
        string url = null,
        int defaultPageSize = 5,
        int maxPageSize = 50,
        string pageSizeQueryParam = "page_size",
        string pageNumberQueryParam = "page") : base(defaultPageSize, maxPageSize)
    {
        _url = url;
        _pageSizeQueryParam = pageSizeQueryParam;
        _pageNumberQueryParam = pageNumberQueryParam;
    }

    public override async Task<Paginated<TDestination>?> PaginateAsync(IQueryable<TDestination> source, HttpRequest request)
    {
        var queryParams = request.Query.ToList();
        _url = _url ?? request.GetDisplayUrl();
        // Extract query strings
        var limitQueryParam = queryParams.FirstOrDefault(pair => pair.Key == _pageSizeQueryParam);
        var pageNumberQueryParam = queryParams.FirstOrDefault(pair => pair.Key == _pageNumberQueryParam);
        var allOthersParams = queryParams
            .Where(pair => pair.Key != _pageSizeQueryParam || pair.Key != _pageNumberQueryParam).ToList();
        // Basic data
        var numberOfRowsToTake = RetrieveConfiguredLimit(limitQueryParam.Value);
        var desiredPageNumber = RetrieveConfiguredPageNumber(pageNumberQueryParam.Value);
        // Building list
        var count = await source.CountAsync();
        if (count == 0) return null;
        var totalNumberOfPages = (int)Math.Ceiling((double)count / numberOfRowsToTake);
        var actualPageNumber = desiredPageNumber > totalNumberOfPages ? totalNumberOfPages : desiredPageNumber;
        var numberOfRowsToSkip = (actualPageNumber - 1) * numberOfRowsToTake;
        var items = await source.Skip(numberOfRowsToSkip).Take(numberOfRowsToTake).ToListAsync();
        // Links
        var nextLink = RetrieveNextLink(desiredPageNumber, totalNumberOfPages, numberOfRowsToTake, allOthersParams);
        var previousLink = RetrievePreviousLink(desiredPageNumber, numberOfRowsToTake, allOthersParams);

        return new Paginated<TDestination>(count, nextLink, previousLink, items);
    }

    private int RetrieveConfiguredPageNumber(StringValues values)
    {
        var value = values.FirstOrDefault();

        if (value is not null)
        {
            var couldBeParsed = int.TryParse(value, out var requestedPageNumberValue);
            if (couldBeParsed && requestedPageNumberValue > 0) return requestedPageNumberValue;
        }

        return 1;
    }

    private string? RetrievePreviousLink(int pageNumber, int numberOfRowsToTake,
        List<KeyValuePair<string, StringValues>> paramsForFiltering)
    {
        var hasPrevious = pageNumber > 1;
        if (!hasPrevious) return null;
        var previousPageNumber = pageNumber - 1;

        // Now we have everything we need to build the next link
        var uriBuilder = new UriBuilder(_url);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        // In case the user has added some filters, let's add them all, even the invalid ones
        foreach (var paramForFiltering in paramsForFiltering)
        {
            var key = paramForFiltering.Key;
            var value = paramForFiltering.Value[0];
            query.Add(key, value);
        }

        query[_pageNumberQueryParam] = previousPageNumber.ToString();
        query[_pageSizeQueryParam] = numberOfRowsToTake.ToString();
        uriBuilder.Query = query.ToString();

        return uriBuilder.Uri.AbsoluteUri;
    }

    private string? RetrieveNextLink(int pageNumber, int totalNumberOfPages, int numberOfRowsToTake,
        List<KeyValuePair<string, StringValues>> paramsForFiltering)
    {
        var hasNext = pageNumber < totalNumberOfPages;
        if (!hasNext) return null;
        var nextPageNumber = pageNumber + 1;

        // Now we have everything we need to build the next link
        var uriBuilder = new UriBuilder(_url);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        // When you add some filters, we must repass the valid ones
        foreach (var paramForFiltering in paramsForFiltering)
        {
            var key = paramForFiltering.Key;
            var value = paramForFiltering.Value[0];
            query.Add(key, value);
        }

        query[_pageNumberQueryParam] = nextPageNumber.ToString();
        query[_pageSizeQueryParam] = numberOfRowsToTake.ToString();
        uriBuilder.Query = query.ToString();

        return uriBuilder.Uri.AbsoluteUri;
    }
}
