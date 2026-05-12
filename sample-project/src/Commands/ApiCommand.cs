using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NDjango.RestFramework.Extensions;
using Newtonsoft.Json;

namespace SampleProject.Commands;

/// <summary>
/// Single API command: wires NDjango.RestFramework, SQL Server, Newtonsoft.Json, and Swagger
/// into a minimal ASP.NET Core host. Controllers live under <c>Controllers/</c>; serializer
/// subclasses (auto-discovered by <c>AddNDjangoRestFramework</c>) live under <c>Serializers/</c>.
/// </summary>
public static class ApiCommand
{
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        // Pin the content root to the binary's output directory so the host's default
        // configuration probe finds the appsettings.json that the csproj links in via
        // <Content Include="../appsettings.json" Link="appsettings.json"> regardless of
        // where `dotnet run` was invoked from.
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(web =>
            {
                web.UseContentRoot(AppContext.BaseDirectory);
                web.UseStartup<Startup>();
            })
            .Build();

        // Convenience for the sample: bring the schema up at boot so endpoints work the first time.
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        await host.RunAsync(cancellationToken);
    }

    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration) => _configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = _configuration.GetConnectionString("AppDbContext");

            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

            // Library wiring: auto-scans this assembly for Serializer<,,,> subclasses and
            // registers them, plus the startup field-validation hosted service.
            services.AddNDjangoRestFramework();

            services.AddControllers(options =>
                {
                    // Without this opt-out, MVC treats every non-nullable reference type
                    // property on the DTOs as implicitly [Required]. The PATCH pipeline
                    // materializes a defaulted PartialJsonObject<T>.Instance before the
                    // library's serializer runs, so the implicit-required check would 400
                    // every partial body that omits a string field. Matches the library's
                    // own test fixture posture.
                    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                })
                .AddNewtonsoftJson(config =>
                {
                    // BaseController renders responses through Newtonsoft.Json — match the
                    // reference loop posture used in the library tests so navigation properties
                    // don't blow up serialization.
                    config.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    config.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SampleProject API",
                    Version = "v1",
                    Description = "Sample consumer of NDjango.RestFramework, used to verify the library by practice."
                });

                // Pull xmldoc from this project (and any siblings that ship it, including the
                // library if it is built with GenerateDocumentationFile) so the OpenAPI schema
                // surfaces the controller and contract notes.
                var xmlDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                foreach (var xmlPath in System.IO.Directory.EnumerateFiles(xmlDir, "*.xml"))
                {
                    try { options.IncludeXmlComments(xmlPath); }
                    catch { /* tolerate non-doc xml files in the output dir */ }
                }
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseRouting();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "SampleProject API v1");
                options.RoutePrefix = "swagger";
            });
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
