using Microsoft.Extensions.DependencyInjection;
using PriceTracker.Application.Services;

namespace PriceTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<UserService>();
        services.AddScoped<TrackedItemService>();
        services.AddScoped<PriceCheckService>();
        return services;
    }
}
