using Swap.Htmx;
//#if (IncludeSampleModule)
using SwapModularMonolith.Modules.Notes.Events;
//#endif

namespace SwapModularMonolith.Infrastructure;

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
            // Auto-suppress layout for HTMX requests - modules don't need their own _ViewStart.cshtml!
            options.AutoSuppressLayout = true;
            
            // Default navigation target for <swap-nav> tag helper
            options.DefaultNavigationTarget = "#main-content";
            
//#if (IncludeSampleModule)
            options.PartialViewSearchPaths.Add("Notes");
            options.AddConfig<NotesEventConfig>();
//#endif
        });

        return services;
    }
}
