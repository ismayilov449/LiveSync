using FluentValidation;
using LiveSync.Application.Common.Behaviors;
using LiveSync.Application.CQRS.Tickets.Services;
using LiveSync.Application.RealTimeSync.Handlers;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.Services;
using LiveSync.Application.RealTimeSync.Subscriptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LiveSync.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<SubscriptionManager>();
        services.AddScoped<ChangeProcessor>();
        services.AddScoped<IChangeHandler, QueueChangeHandler>();
        services.AddScoped<IChangeHandler, TicketChangeHandler>();
        services.AddScoped<ITicketQueueValidator, TicketQueueValidator>();

        return services;
    }
}
