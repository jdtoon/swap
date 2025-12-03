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
//#if (IncludeSampleModule)
            options.PartialViewSearchPaths.Add("Notes");
            options.AddConfig<NotesEventConfig>();
//#endif
        });

        return services;
    }
}
