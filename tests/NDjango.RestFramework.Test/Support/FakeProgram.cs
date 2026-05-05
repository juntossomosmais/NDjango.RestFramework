using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NDjango.RestFramework.Extensions;
using NDjango.RestFramework.Serializer;
using NDjango.RestFramework.Test.Support;
using Newtonsoft.Json;

void InitializeDatabase(IHost app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureDeleted();

    var (sellers, customers, customerDocuments) = FakeDataGenerator.GenerateFakeData();

    dbContext.Seller.AddRange(sellers);
    dbContext.Customer.AddRange(customers);
    dbContext.CustomerDocument.AddRange(customerDocuments);

    dbContext.SaveChanges();
}

var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions()
{
    ContentRootPath = Directory.GetCurrentDirectory(),
});

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();
builder.Configuration.AddConfiguration(configuration);

// Add services to the container.
var assembly = typeof(SellersController).Assembly;
builder.Services.AddControllers(options =>
    {
        // The test project has <Nullable>enable</Nullable>; without this opt-out, MVC would
        // treat every non-nullable reference type property on the test DTOs (e.g.,
        // CustomerDto.Name, CustomerDto.CustomerDocuments) as implicitly [Required] and
        // reject any POST that omits them at model-binding time — before our serializer
        // pipeline runs. Disable the implicit rule so validation behavior stays driven by
        // explicit [Required] / DataAnnotations and the per-field Validate{X}Async hooks.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddNewtonsoftJson(config =>
    {
        config.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        config.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    })
    .PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
// The fixture intentionally registers controllers with bad fields (SellersController,
// InvalidFieldEntitiesController, InvalidAllowedFieldEntitiesController) so the hosted
// service tests can exercise the failure path. We opt out of startup validation so the
// host can boot, while still getting the response factory + serializer scan that
// AddNDjangoRestFramework normally provides.
builder.Services.AddNDjangoRestFramework(opts => opts.RunStartupValidation = false);
builder.Services.AddHttpContextAccessor();
var defaultConnectionString =
    "Data Source=localhost,1433;Initial Catalog=REPLACE_ME_PROGRAMATICALLY;User Id=sa;Password=Password1;TrustServerCertificate=True";
var connectionString = configuration.GetConnectionString("AppDbContext") ?? defaultConnectionString;
connectionString = connectionString.Replace("REPLACE_ME_PROGRAMATICALLY", Guid.NewGuid().ToString());
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString), ServiceLifetime.Singleton);
// Context.Database.EnsureCreated();
// Rest framework configuration
builder.Services.AddScoped<CustomerSerializer>();
builder.Services.AddScoped<ValidatingCustomerSerializer>();
builder.Services.AddScoped<ThrowingCustomerSerializer>();
builder.Services.AddScoped<PerFieldCustomerSerializer>();
builder.Services.AddScoped<PerFieldWithCrossFieldSerializer>();
builder.Services.AddScoped<PerFieldShortCircuitSerializer>();
builder.Services.AddSingleton<ContextCapturingSerializer>();
builder.Services.AddScoped<MisnamedHookSerializer>();
builder.Services.AddScoped<Serializer<SellerDto, Seller, Guid, AppDbContext>>();
builder.Services.AddScoped<Serializer<CustomerDocumentDto, CustomerDocument, Guid, AppDbContext>>();
builder.Services.AddScoped<Serializer<IntAsIdEntityDto, IntAsIdEntity, int, AppDbContext>>();
builder.Services.AddScoped<Serializer<InvalidFieldEntityDto, InvalidFieldEntity, Guid, AppDbContext>>();

// Configure the HTTP request pipeline.
var app = builder.Build();
app.UseAuthorization();
app.MapControllers();
app.UseCors(policyBuilder =>
{
    policyBuilder.AllowAnyHeader();
    policyBuilder.AllowAnyMethod();
    policyBuilder.AllowAnyOrigin();
});
app.MapGet("/debug/routes", (IActionDescriptorCollectionProvider provider) =>
{
    return provider.ActionDescriptors.Items.Select(x => new
    {
        Action = x.RouteValues["Action"],
        Method = x.ActionConstraints.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods.FirstOrDefault(),
        Controller = x.RouteValues["Controller"],
        Name = x.AttributeRouteInfo.Name,
        Template = x.AttributeRouteInfo.Template
    }).ToList();
});

// Ensure a dedicated database is created for each test method
// TODO: This is executed twice. The idea is to move it to `IntegrationTests` and make it work properly
app.Services.GetRequiredService<AppDbContext>().Database.EnsureCreated();

await app.RunAsync();

// Allows tests with WebApplicationFactory
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0#basic-tests-with-the-default-webapplicationfactory-1
public partial class FakeProgram
{
}
