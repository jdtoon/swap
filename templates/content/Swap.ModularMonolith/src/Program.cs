using SwapModularMonolith.Infrastructure;
using SwapModularMonolith.Data;
//#if (IncludeSampleModule)
using SwapModularMonolith.Modules.Notes;
//#endif
//#if (IncludeSse)
using Swap.Htmx.Realtime;
//#endif

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// INFRASTRUCTURE SERVICES
// =============================================================================

builder.Services.AddDataProtectionConfig(builder.Environment);
builder.Services.AddCompressionConfig();

// =============================================================================
// DATABASE & CORE SERVICES
// =============================================================================

builder.Services.AddDatabaseConfig(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
builder.Services.AddCoreServices();

// =============================================================================
// DOMAIN MODULES
// =============================================================================

//#if (IncludeSampleModule)
builder.Services.AddNotesModule();
//#endif

// =============================================================================
// MVC & WEB
// =============================================================================

builder.Services.AddWebOptimizerConfig(builder.Environment);
builder.Services.AddMvcConfig();
builder.Services.AddSwapHtmxConfig();
//#if (IncludeSse)
builder.Services.AddSseEventBridge();
//#endif

// =============================================================================
// BUILD & RUN
// =============================================================================

var app = builder.Build();

await app.InitializeDatabaseAsync();
app.ConfigurePipeline();
//#if (IncludeSse)
app.UseSseEventBridge();
//#endif
app.MapEndpoints();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
