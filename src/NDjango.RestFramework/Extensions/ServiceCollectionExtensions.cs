using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Serializer;
using NDjango.RestFramework.Validation;

namespace NDjango.RestFramework.Extensions;

/// <summary>
/// Configuration knobs for <see cref="ServiceCollectionExtensions.AddNDjangoRestFramework"/>.
/// </summary>
public sealed class NDjangoRestFrameworkOptions
{
    /// <summary>
    /// When <c>true</c> (default), registers the startup hosted service that asserts every
    /// <c>GetFields()</c> entry, <c>AllowedFields</c> entry, and <c>Validate{X}Async</c> hook
    /// resolves to a real property — failing host start if any name is wrong.
    /// </summary>
    /// <remarks>
    /// Set to <c>false</c> only when you intentionally need to register misconfigured
    /// controllers (test fixtures that exercise the failure path), or when you have a hard
    /// constraint against host-start probing.
    /// </remarks>
    public bool RunStartupValidation { get; set; } = true;

    /// <summary>
    /// Assemblies to scan for non-abstract <see cref="Serializer{TOrigin, TDestination, TPrimaryKey, TContext}"/>
    /// subclasses. Empty by default — when empty, the calling assembly is used.
    /// </summary>
    public IList<Assembly> Assemblies { get; } = new List<Assembly>();
}

/// <summary>
/// Single-call wiring for NDjango.RestFramework.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly Type _openSerializer = typeof(Serializer<,,,>);

    /// <summary>
    /// Wires the library into the service collection in one call:
    /// <list type="bullet">
    ///   <item>
    ///     Scans the configured assemblies (default: caller assembly) for non-abstract
    ///     subclasses of <see cref="Serializer{TOrigin, TDestination, TPrimaryKey, TContext}"/>
    ///     and registers each as <see cref="ServiceLifetime.Scoped"/> against both its concrete
    ///     type and its closed-generic base. Manual registrations made before this call win
    ///     (uses <c>TryAdd</c>).
    ///   </item>
    ///   <item>
    ///     Registers the startup field-validation hosted service: <c>GetFields()</c>,
    ///     <c>AllowedFields</c>, and per-field <c>Validate{X}Async</c> hook names must all
    ///     resolve to real properties; otherwise <c>StartAsync</c> throws and the host fails
    ///     to start. Disable via <see cref="NDjangoRestFrameworkOptions.RunStartupValidation"/>.
    ///   </item>
    ///   <item>
    ///     Configures <see cref="ApiBehaviorOptions.InvalidModelStateResponseFactory"/> so that
    ///     <c>[ApiController]</c>'s automatic 400 from invalid <c>ModelState</c> emits the
    ///     library's <see cref="ValidationErrors"/> shape.
    ///   </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// When two non-abstract serializer subclasses share the same closed-generic base, the
    /// first one scanned wins for the closed-base mapping; the others still register as
    /// concrete types. To override, register the closed base manually before calling this method.
    /// </remarks>
    public static IServiceCollection AddNDjangoRestFramework(
        this IServiceCollection services,
        Action<NDjangoRestFrameworkOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var options = new NDjangoRestFrameworkOptions();
        configure?.Invoke(options);

        var assembliesToScan = options.Assemblies.Count == 0
            ? new[] { Assembly.GetCallingAssembly() }
            : options.Assemblies.Distinct().ToArray();

        RegisterSerializers(services, assembliesToScan);

        if (options.RunStartupValidation)
            services.AddHostedService<ControllerFieldValidationHostedService>();

        ConfigureValidationResponseFactory(services);

        return services;
    }

    /// <summary>
    /// Configures the validation response factory in isolation from the rest of the wiring.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="OptionsServiceCollectionExtensions.PostConfigure{TOptions}(IServiceCollection, Action{TOptions})"/>
    /// rather than <c>Configure</c>: MVC's default factory is registered through
    /// <c>ApiBehaviorOptionsSetup</c>, an <see cref="Microsoft.Extensions.Options.IConfigureOptions{TOptions}"/>
    /// that always assigns a built-in ProblemDetails factory during the <c>Configure</c> phase.
    /// A plain <c>Configure</c> registration on our side races with that setup based on the
    /// consumer's call order — calling <c>AddControllers()</c> after our extension would
    /// silently overwrite the library's factory. <c>PostConfigure</c> runs strictly after
    /// every <c>Configure</c>, so our factory wins regardless of registration order.
    /// </remarks>
    private static void ConfigureValidationResponseFactory(IServiceCollection services)
    {
        services.PostConfigure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = new Dictionary<string, string[]>();
                foreach (var (key, value) in context.ModelState)
                    errors.Add(key, value.Errors.Select(e => e.ErrorMessage).ToArray());

                return new BadRequestObjectResult(new ValidationErrors(errors));
            };
        });
    }

    private static void RegisterSerializers(IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies.Distinct())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Tolerant of partially-loadable assemblies — register what we can resolve.
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (var type in types)
            {
                if (type is null || !type.IsClass || type.IsAbstract)
                    continue;

                var closedBase = ResolveClosedSerializerBase(type);
                if (closedBase is null)
                    continue;

                services.TryAddScoped(type);
                services.TryAddScoped(closedBase, type);
            }
        }
    }

    private static Type? ResolveClosedSerializerBase(Type type)
    {
        var current = type.BaseType;
        while (current != null && current != typeof(object))
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == _openSerializer)
                return current;
            current = current.BaseType;
        }
        return null;
    }
}
