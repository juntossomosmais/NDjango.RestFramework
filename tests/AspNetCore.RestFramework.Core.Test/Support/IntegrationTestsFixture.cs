using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AspNetCore.RestFramework.Core.Test.Support;

public class IntegrationTests
{
    protected readonly HttpClient Client;
    protected readonly AppDbContext Context;

    protected IntegrationTests()
    {
        WebApplicationFactory<FakeProgram> factory = new();
        Client = factory.CreateClient();
        Context = factory.Services.GetRequiredService<AppDbContext>();
    }
}
