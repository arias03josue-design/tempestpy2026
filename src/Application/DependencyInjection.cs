using System.Globalization;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tempest.Application.Common.Behaviors;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Security;
using Tempest.Application.Users;

namespace Tempest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ISender, Dispatcher>();
        services.AddScoped<CurrentUserAccessor>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserAccessor>());
        services.AddScoped<IUserSessionValidator, UserSessionValidator>();

        RegisterHandlers(services, assembly);

        // Pipeline order: the first registered behavior is the outermost one.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Custom messages are in Spanish; built-in fallbacks too.
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("es");
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Singleton, includeInternalTypes: true);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerContract = typeof(IRequestHandler<,>);

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
            {
                continue;
            }

            foreach (var contract in type.GetInterfaces())
            {
                if (contract.IsGenericType && contract.GetGenericTypeDefinition() == handlerContract)
                {
                    services.AddScoped(contract, type);
                }
            }
        }
    }
}
