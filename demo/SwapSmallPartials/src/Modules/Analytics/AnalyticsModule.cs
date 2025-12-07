using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics;

public static class AnalyticsModule
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        // Register the analytics state as a singleton to maintain state across requests
        services.AddSingleton<AnalyticsState>();
        
        return services;
    }
}
