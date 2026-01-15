using WebOptimizer;

namespace SwapModularMonolith.Infrastructure;

public static class WebOptimizerExtensions
{
    public static IServiceCollection AddWebOptimizerConfig(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddWebOptimizer(pipeline =>
        {
            // Bundle DaisyUI + custom theme CSS
            pipeline.AddCssBundle("/css/styles.css",
                "lib/daisyui/daisyui.css",
                "lib/daisyui/themes.css",
                "css/theme.css"
            );

            // Bundle Tailwind + JS files
            pipeline.AddJavaScriptBundle("/js/bundle.js",
                "lib/tailwindcss/dist/index.global.min.js",
                "js/layout.js"
            );

            // Minify in production
            if (!environment.IsDevelopment())
            {
                pipeline.MinifyCssFiles();
                pipeline.MinifyJsFiles();
            }
        });

        return services;
    }
}
