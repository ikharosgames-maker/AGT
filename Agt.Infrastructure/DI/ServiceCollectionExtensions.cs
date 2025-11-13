// Agt.Infrastructure/DI/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Agt.Domain.Abstractions;
using Agt.Domain.Repositories;
using Agt.Application.Services;

namespace Agt.Infrastructure.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgtCore(this IServiceCollection services, bool useJsonStore)
    {
        // Application Services
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IRoutingService, RoutingService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IProcessDefinitionService, ProcessDefinitionService>();
        services.AddSingleton<IAuthZ, AuthZ>();

        if (useJsonStore)
        {
            services.AddSingleton<IFormRepository, JsonStore.JsonFormRepository>();
            services.AddSingleton<IBlockRepository, JsonStore.JsonBlockRepository>();
            services.AddSingleton<ICaseRepository, JsonStore.JsonCaseRepository>();
            services.AddSingleton<IRouteRepository, JsonStore.JsonRouteRepository>();
            services.AddSingleton<ITaskRepository, JsonStore.JsonTaskRepository>();
            services.AddSingleton<INotificationRepository, JsonStore.JsonNotificationRepository>();
        }
        else
        {
            // TODO: SQLite/EF varianta
        }

        return services;
    }
}
