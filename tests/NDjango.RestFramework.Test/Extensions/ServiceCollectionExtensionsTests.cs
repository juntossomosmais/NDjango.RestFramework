using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NDjango.RestFramework.Extensions;
using NDjango.RestFramework.Serializer;
using NDjango.RestFramework.Test.Support;
using NDjango.RestFramework.Validation;
using Xunit;

namespace NDjango.RestFramework.Test.Extensions;

public class ServiceCollectionExtensionsTests
{
    public class AddNDjangoRestFrameworkSerializerScanning
    {
        [Fact]
        public void AddNDjangoRestFramework_WhenAssemblyHasSerializerSubclasses_ShouldRegisterEachConcreteType()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<AppDbContext>(_ => null!);

            // Act
            services.AddNDjangoRestFramework(opts => opts.Assemblies.Add(typeof(CustomerSerializer).Assembly));

            // Assert
            Assert.Contains(services, d =>
                d.ServiceType == typeof(CustomerSerializer)
                && d.ImplementationType == typeof(CustomerSerializer)
                && d.Lifetime == ServiceLifetime.Scoped);
            Assert.Contains(services, d =>
                d.ServiceType == typeof(ValidatingCustomerSerializer)
                && d.ImplementationType == typeof(ValidatingCustomerSerializer)
                && d.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddNDjangoRestFramework_WhenSubclassFound_ShouldAlsoRegisterClosedGenericBase()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<AppDbContext>(_ => null!);

            // Act
            services.AddNDjangoRestFramework(opts => opts.Assemblies.Add(typeof(IsolatedFakeSerializer).Assembly));

            // Assert
            // IsolatedFakeSerializer is the only direct subclass of Serializer<IsolatedFakeDto, ..., int, AppDbContext>
            // so it should claim the closed-base mapping.
            Assert.Contains(services, d =>
                d.ServiceType == typeof(Serializer<IsolatedFakeDto, IsolatedFakeEntity, int, AppDbContext>)
                && d.ImplementationType == typeof(IsolatedFakeSerializer)
                && d.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddNDjangoRestFramework_WhenManualRegistrationExists_ShouldNotOverride()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<AppDbContext>(_ => null!);
            // Pre-register the closed base with a different concrete type to test TryAdd semantics.
            services.AddScoped<Serializer<CustomerDto, Customer, System.Guid, AppDbContext>, ValidatingCustomerSerializer>();

            // Act
            services.AddNDjangoRestFramework(opts => opts.Assemblies.Add(typeof(CustomerSerializer).Assembly));

            // Assert
            // The manual registration must win — the scan should not overwrite the closed-base mapping.
            var closedBaseDescriptors = services
                .Where(d => d.ServiceType == typeof(Serializer<CustomerDto, Customer, System.Guid, AppDbContext>))
                .ToList();
            Assert.Single(closedBaseDescriptors);
            Assert.Equal(typeof(ValidatingCustomerSerializer), closedBaseDescriptors[0].ImplementationType);
        }

        [Fact]
        public void AddNDjangoRestFramework_ShouldSkipAbstractSerializerSubclasses()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<AppDbContext>(_ => null!);

            // Act
            services.AddNDjangoRestFramework(opts => opts.Assemblies.Add(typeof(AbstractFakeSerializer).Assembly));

            // Assert
            Assert.DoesNotContain(services, d => d.ImplementationType == typeof(AbstractFakeSerializer));
        }
    }

    public class AddNDjangoRestFrameworkHostedService
    {
        [Fact]
        public void AddNDjangoRestFramework_WhenRunStartupValidationDefaultTrue_ShouldRegisterControllerFieldValidationHostedService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNDjangoRestFramework(opts => opts.Assemblies.Add(typeof(CustomerSerializer).Assembly));

            // Assert
            Assert.Contains(services, d =>
                d.ServiceType == typeof(IHostedService)
                && d.ImplementationType == typeof(ControllerFieldValidationHostedService));
        }

        [Fact]
        public void AddNDjangoRestFramework_WhenRunStartupValidationFalse_ShouldNotRegisterHostedService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNDjangoRestFramework(opts =>
            {
                opts.RunStartupValidation = false;
                opts.Assemblies.Add(typeof(CustomerSerializer).Assembly);
            });

            // Assert
            Assert.DoesNotContain(services, d => d.ImplementationType == typeof(ControllerFieldValidationHostedService));
        }
    }

    public class AddNDjangoRestFrameworkResponseFactory
    {
        [Fact]
        public void AddNDjangoRestFramework_ShouldConfigureInvalidModelStateResponseFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions<ApiBehaviorOptions>();

            // Act
            services.AddNDjangoRestFramework(opts => opts.Assemblies.Add(typeof(CustomerSerializer).Assembly));

            // Assert
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            Assert.NotNull(options.InvalidModelStateResponseFactory);
        }

        [Fact]
        public void AddNDjangoRestFramework_WhenConsumerConfiguresFactoryAfterUs_ShouldStillWin()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions<ApiBehaviorOptions>();

            // Act
            // Library wires its factory first, then a consumer-side Configure registers a
            // sentinel factory. PostConfigure must still run after — the library's factory wins.
            services.AddNDjangoRestFramework(opts => opts.Assemblies.Add(typeof(CustomerSerializer).Assembly));
            services.Configure<ApiBehaviorOptions>(o =>
                o.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult("consumer-sentinel"));

            // Assert
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            // Force-invoke the factory with a stub ActionContext to verify it's the library's, not the sentinel.
            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(),
            };
            var result = options.InvalidModelStateResponseFactory(actionContext);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<NDjango.RestFramework.Errors.ValidationErrors>(badRequest.Value);
        }
    }

    public class AddNDjangoRestFrameworkAssemblyDefaults
    {
        [Fact]
        public void AddNDjangoRestFramework_WhenNoAssembliesProvided_ShouldDefaultToCallingAssembly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<AppDbContext>(_ => null!);

            // Act
            // No configure delegate — defaults: empty Assemblies list -> caller assembly,
            // which is this test assembly and contains all the test Support serializers.
            services.AddNDjangoRestFramework();

            // Assert
            Assert.Contains(services, d => d.ImplementationType == typeof(CustomerSerializer));
        }
    }
}

#region Test fixtures isolated to this file

// Standalone serializer with a unique closed base, so the "registers closed base"
// test does not collide with the many CustomerDto-based serializers in Support/Serializers.cs.
public class IsolatedFakeDto : NDjango.RestFramework.Base.BaseDto<int>
{
    public string? Name { get; set; }
}

public class IsolatedFakeEntity : NDjango.RestFramework.Base.BaseModel<int>
{
    public string? Name { get; set; }
    public override string[] GetFields() => new[] { "Id", "Name" };
}

public class IsolatedFakeSerializer : Serializer<IsolatedFakeDto, IsolatedFakeEntity, int, AppDbContext>
{
    public IsolatedFakeSerializer(AppDbContext ctx) : base(ctx) { }
}

// Used to verify that abstract subclasses are not registered.
public abstract class AbstractFakeSerializer : Serializer<IsolatedFakeDto, IsolatedFakeEntity, int, AppDbContext>
{
    protected AbstractFakeSerializer(AppDbContext ctx) : base(ctx) { }
}

#endregion
