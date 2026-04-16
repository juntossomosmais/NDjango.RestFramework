using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NDjango.RestFramework.Test.Support;

public class IntegrationTests
{
    protected readonly HttpClient Client;
    protected readonly AppDbContext Context;
    protected readonly IServiceProvider Services;

    protected IntegrationTests()
    {
        WebApplicationFactory<FakeProgram> factory = new();
        Client = factory.CreateClient();
        Services = factory.Services;
        Context = factory.Services.GetRequiredService<AppDbContext>();
        Context.Database.EnsureCreated();
    }
}
