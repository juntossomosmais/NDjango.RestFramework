using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using NDjango.RestFramework.Paginations;
using NDjango.RestFramework.Test.Support;
using Xunit;

namespace NDjango.RestFramework.Test.Paginations;

public class Thing
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string Name { get; set; }
    public string Greetings { get; set; }
    public bool Robot { get; set; }

    public Thing(int id, string name, string greetings, bool robot)
    {
        Id = id;
        Name = name;
        Greetings = greetings;
        Robot = robot;
    }
}

public class PageNumberPaginationTests
{
    private readonly DbContextBuilder.TestDbContext<Thing> _dbContext;
    private readonly PageNumberPagination<Thing> _pagination;
    private readonly int _defaultPageSize;
    private readonly string _url;

    public PageNumberPaginationTests()
    {
        _defaultPageSize = 5;
        _dbContext = DbContextBuilder.CreateDbContext<Thing>();
        _dbContext.Database.EnsureCreated();
        _url = "https://your-honest-address";
        _pagination = new PageNumberPagination<Thing>(url: _url);
    }

    [Fact(DisplayName = "When no options such as `page` or offset are provided")]
    public async Task Options1()
    {
        // Arrange
        var query = await CreateScenarioWith50Things(_dbContext);
        var mockHttpRequest = new Mock<HttpRequest>();
        var queryParams = Http.RetrieveQueryCollectionFromQueryString(string.Empty);
        mockHttpRequest.Setup(req => req.Query).Returns(queryParams);
        // Act
        var paginated = await _pagination.PaginateAsync(query, mockHttpRequest.Object);
        // Assert
        Assert.Equal(50, paginated.Count);
        Assert.Equal(_defaultPageSize, paginated.Results.Count());
        Assert.Null(paginated.Previous);
        var expectedNextPage = 2;
        var expectedNext = $"{_url}/?page={expectedNextPage}&page_size={_defaultPageSize}";
        Assert.Equal(expectedNext, paginated.Next);
    }

    [Fact(DisplayName = "When the source queryset is empty, emits the {count:0, next:null, previous:null, results:[]} envelope")]
    public async Task PaginateAsync_WithEmptySource_ShouldReturnEmptyEnvelope()
    {
        // Arrange — no rows added. Mirrors DRF's PageNumberPagination which builds an empty
        // Page via Django's Paginator.page(1) and renders it through the same
        // get_paginated_response() branch as a populated page (rest_framework/pagination.py
        // :220-226 + mixins.py:34-44 at encode/django-rest-framework@3.17.1).
        var query = _dbContext.Entities.AsNoTracking().OrderBy(p => p.Id);
        var mockHttpRequest = new Mock<HttpRequest>();
        var queryParams = Http.RetrieveQueryCollectionFromQueryString(string.Empty);
        mockHttpRequest.Setup(req => req.Query).Returns(queryParams);

        // Act
        var paginated = await _pagination.PaginateAsync(query, mockHttpRequest.Object);

        // Assert — payload (not null) with explicit zero count and empty results.
        Assert.NotNull(paginated);
        Assert.Equal(0, paginated.Count);
        Assert.Empty(paginated.Results);
        Assert.Null(paginated.Next);
        Assert.Null(paginated.Previous);
    }

    private static async Task<IQueryable<Thing>> CreateScenarioWith50Things(DbContextBuilder.TestDbContext<Thing> dbContext)
    {
        var helloWords = new[]
        {
            "Bonjour",
            "Hola",
            "Salve",
            "Guten Tag",
            "Olá",
            "Anyoung haseyo",
            "Goedendag",
            "Yassas",
            "Shalom",
            "God dag",
        };
        var indexForHelloWords = 0;
        var things = new List<Thing>();

        foreach (var index in Enumerable.Range(1, 50))
        {
            var greetingsMessage = helloWords[indexForHelloWords++];
            var IsRobot = index % 2 == 0;

            var thing = new Thing(index, $"Thing {index}", greetingsMessage, IsRobot);
            things.Add(thing);

            var shouldRestartIndexForHelloWords = index % 10 == 0;
            if (shouldRestartIndexForHelloWords)
                indexForHelloWords = 0;
        }

        await dbContext.AddRangeAsync(things);
        await dbContext.SaveChangesAsync();

        // https://docs.microsoft.com/en-us/ef/core/querying/tracking#no-tracking-queries
        return dbContext.Entities.AsNoTracking().OrderBy(p => p.Id);
    }
}
