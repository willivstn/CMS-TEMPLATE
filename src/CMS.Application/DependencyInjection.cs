using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Application;

/// <summary>
/// Registers the Application layer: MediatR (with cross-cutting pipeline
/// behaviours), FluentValidation validators, and source-generated Mapperly
/// mappers. Called from the Web composition root.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(IApplicationMarker).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Cross-cutting behaviours run in registration order around handlers.
            cfg.AddOpenBehavior(typeof(Common.Behaviors.ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        RegisterMappers(services, assembly);

        return services;
    }

    /// <summary>
    /// Registers all Mapperly-generated mapper classes (concrete, non-abstract
    /// types whose name ends in "Mapper") as singletons against their interfaces.
    /// Mapperly mappers are stateless, making singleton lifetime safe.
    /// </summary>
    private static void RegisterMappers(IServiceCollection services, Assembly assembly)
    {
        var mapperTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Mapper", StringComparison.Ordinal));

        foreach (var mapper in mapperTypes)
        {
            foreach (var contract in mapper.GetInterfaces())
            {
                services.AddSingleton(contract, mapper);
            }

            services.AddSingleton(mapper);
        }
    }
}
