using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace NDjango.RestFramework.Paginations;

public record Paginated<TDestination>(int? Count, string? Next, string? Previous, IEnumerable<TDestination> Results);

public record PaginatedResponse<TDestination>(int? Count, string? Next, string? Previous, TDestination Results); // TODO: Remove it

public interface IPagination<TDestination>
{
    public Task<Paginated<TDestination>?> ListAsync(IQueryable<TDestination> source, HttpRequest queryParams);
}

public abstract class Pagination<TDestination> : IPagination<TDestination>
{
    protected readonly int _defaultLimit;
    protected readonly int _maxPageSize;

    protected Pagination(int defaultPageSize, int maxPageSize)
    {
        _defaultLimit = defaultPageSize;
        _maxPageSize = maxPageSize;
    }

    protected int RetrieveConfiguredLimit(StringValues values)
    {
        var value = values.FirstOrDefault();

        if (value is not null)
        {
            int requestedLimitValue;
            var couldBeParsed = int.TryParse(value, out requestedLimitValue);

            if (couldBeParsed && requestedLimitValue > 0)
            {
                var valueToBeReturned = requestedLimitValue > _maxPageSize ? _maxPageSize : requestedLimitValue;
                return valueToBeReturned;
            }
        }

        return _defaultLimit;
    }

    public abstract Task<Paginated<TDestination>?> ListAsync(IQueryable<TDestination> source,
        HttpRequest queryParams);
}
