using AspNetRestFramework.Sample;
using AspNetRestFramework.Sample.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using Xunit;

namespace AspNetCore.RestFramework.Test
{
    [Collection("Database Collection")]
    public abstract class IntegrationTestBase
    {
        protected IntegrationTestBase()
        {
            var applicationPath = Directory.GetCurrentDirectory();

            Server = new TestServer(new WebHostBuilder()
                .UseEnvironment("Testing")
                .UseContentRoot(applicationPath)
                .UseConfiguration(new ConfigurationBuilder()
                    .SetBasePath(applicationPath)
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build()
                )
                .UseStartup<Startup>());

            Context = (ApplicationDbContext)Server.Services.GetService(typeof(ApplicationDbContext));

            Client = Server.CreateClient();

            ClearDatabase();
        }

        protected TestServer Server { get; }
        protected ApplicationDbContext Context { get; }
        protected HttpClient Client { get; }

        protected void ClearDatabase()
        {
            Context.Database.ExecuteSqlRaw(@"
                EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all';
                EXEC sp_msforeachtable 'DELETE FROM ?';
                EXEC sp_msforeachtable 'ALTER TABLE ? CHECK CONSTRAINT all';
            ");
        }
    }
}
