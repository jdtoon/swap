using Swap.Htmx;
using SwapSmallPartials.Modules.Notes.Events;
using SwapSmallPartials.Modules.Partials.Events;

namespace SwapSmallPartials.Infrastructure;

public static class MvcExtensions
{
    public static IServiceCollection AddMvcConfig(this IServiceCollection services)
    {
        services.AddControllersWithViews(options =>
        {
            options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
        });

        services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
        {
            options.AddModuleViewLocations();
        });

        return services;
    }

    public static IServiceCollection AddSwapHtmxConfig(this IServiceCollection services)
    {
        services.AddSwapHtmx(options =>
        {
            // Default navigation target for <swap-nav> tag helper
            options.DefaultNavigationTarget = "#main-content";
            
            options.PartialViewSearchPaths.Add("Notes");
            options.PartialViewSearchPaths.Add("Partials");
            options.AddConfig<NotesEventConfig>();
            options.AddConfig<PartialsEventConfig>();
        });

        return services;
    }
}
