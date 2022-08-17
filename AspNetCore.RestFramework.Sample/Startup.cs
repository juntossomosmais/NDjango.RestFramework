using System;
using AspNetCore.RestFramework.Core.Extensions;
using AspNetCore.RestFramework.Core.Serializer;
using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.CustomSerializers;
using AspNetRestFramework.Sample.DTO;
using AspNetRestFramework.Sample.DTO.Validators;
using AspNetRestFramework.Sample.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using JSM.FluentValidation.AspNet.AsyncFilter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace AspNetRestFramework.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>();

            services.AddControllers()
                .AddNewtonsoftJson(config =>
            {
                config.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                config.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            })
            .AddModelValidationAsyncActionFilter(options =>
            {
                options.OnlyApiController = true;
            })
            .ConfigureValidationResponseFormat();

            services.AddHttpContextAccessor();

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<CustomerDtoValidator>();

            var connectionString = Configuration.GetConnectionString("DefaultConnectionString");
            services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlServer(connectionString));

            services.AddScoped<CustomerSerializer>();
            services.AddScoped<Serializer<SellerDto, Seller, Guid, ApplicationDbContext>>();
            services.AddScoped<Serializer<CustomerDocumentDto, CustomerDocument, Guid, ApplicationDbContext>>();
            services.AddScoped<Serializer<IntAsIdEntityDto, IntAsIdEntity, int, ApplicationDbContext>>();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AspNetCore.RestFramework.Sample", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AspNetCore.RestFramework.Sample v1"));

            app.UseCors(c =>
            {
                c.AllowAnyHeader();
                c.AllowAnyMethod();
                c.AllowAnyOrigin();
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
