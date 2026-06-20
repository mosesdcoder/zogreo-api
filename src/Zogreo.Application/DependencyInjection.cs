using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;

namespace Zogreo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, bool isDevelopment, IConfiguration config)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register ISender
        services.AddScoped<ISender, Sender>();

        // Register IEnvironmentInfo
        var exposeOtp = config.GetValue<bool>("App:ExposeOtp");
        services.AddSingleton<IEnvironmentInfo>(new EnvironmentInfo(isDevelopment, exposeOtp));

        // Register all command and query handlers by scanning for closed generic interfaces
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                             i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .Select(i => new { Service = i, Implementation = t }));

        foreach (var h in handlerTypes)
            services.AddScoped(h.Service, h.Implementation);

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    private record EnvironmentInfo(bool IsDevelopment, bool ExposeOtp) : IEnvironmentInfo;
}
