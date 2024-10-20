using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
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
        var queryParams = Http.RetrieveQueryCollectionFromQueryString(String.Empty);
        mockHttpRequest.Setup(req => req.Query).Returns(queryParams);
        // Act
        var paginated = await _pagination.PaginateAsync(query, mockHttpRequest.Object);
        // Assert
        paginated.Count.Should().Be(50);
        paginated.Results.Should().HaveCount(_defaultPageSize);
        paginated.Previous.Should().BeNull();
        var expectedNextPage = 2;
        var expectedNext = $"{_url}/?page={expectedNextPage}&page_size={_defaultPageSize}";
        paginated.Next.Should().Be(expectedNext);
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

        foreach (int index in Enumerable.Range(1, 50))
        {
            var greetingsMessage = helloWords[indexForHelloWords++];
            var IsRobot = index % 2 == 0;

            var thing = new Thing(index, $"Thing {index}", greetingsMessage, IsRobot);
            things.Add(thing);

            var shouldRestartIndexForHelloWords = index % 10 == 0;
            if (shouldRestartIndexForHelloWords) indexForHelloWords = 0;
        }

        await dbContext.AddRangeAsync(things);
        await dbContext.SaveChangesAsync();

        // https://docs.microsoft.com/en-us/ef/core/querying/tracking#no-tracking-queries
        return dbContext.Entities.AsNoTracking().OrderBy(p => p.Id);
    }
}
