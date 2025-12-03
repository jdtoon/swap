using WebOptimizer;

namespace SwapModularMonolith.Infrastructure;

public static class WebOptimizerExtensions
{
    public static IServiceCollection AddWebOptimizerConfig(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddWebOptimizer(pipeline =>
        {
            // Bundle all CSS files
            pipeline.AddCssBundle("/css/bundle.css",
                "css/base.css",
                "css/layout.css",
                "css/components.css",
                "css/forms.css"
            );

            // Bundle all JS files
            pipeline.AddJavaScriptBundle("/js/bundle.js",
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
