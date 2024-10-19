using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace NDjango.RestFramework.Test.Support;



public static class DbContextBuilder
{
    public class TestDbContext<TEntity> : DbContext where TEntity : class
    {
        public DbSet<TEntity> Entities { get; set; }

        public TestDbContext(DbContextOptions<TestDbContext<TEntity>> options) : base(options)
        {
        }
    }

    public static TestDbContext<T> CreateDbContext<T>() where T : class
    {
        // Retrieve the connection string
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var defaultConnectionString =
            "Data Source=localhost,1433;Initial Catalog=REPLACE_ME_PROGRAMATICALLY;User Id=sa;Password=Password1;TrustServerCertificate=True";
        var connectionString = configuration.GetConnectionString("AppDbContext") ?? defaultConnectionString;
        connectionString = connectionString.Replace("REPLACE_ME_PROGRAMATICALLY", Guid.NewGuid().ToString());
        // Create the database context
        var dbContextOptions = new DbContextOptionsBuilder<TestDbContext<T>>().UseSqlServer(connectionString).Options;
        return new TestDbContext<T>(dbContextOptions);
    }
}
