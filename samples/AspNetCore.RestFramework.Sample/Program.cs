using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.FakeData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace AspNetRestFramework.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = CreateHostBuilder(args).Build();

            InitializeDatabase(app);

            app.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static void InitializeDatabase(IHost app)
        {
            using (var scope = app.Services.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.Migrate();

                var (sellers, customers, customerDocuments) = FakeDataGenerator.GenerateFakeData();

                dbContext.Seller.AddRange(sellers);
                dbContext.Customer.AddRange(customers);
                dbContext.CustomerDocument.AddRange(customerDocuments);

                dbContext.SaveChanges();
            }
        }
    }
}
