using SwapSmallPartials.Infrastructure;
using SwapSmallPartials.Data;
using SwapSmallPartials.Modules.Notes;
using SwapSmallPartials.Modules.Partials;
using SwapSmallPartials.Modules.Analytics;
using Swap.Htmx;

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

builder.Services.AddNotesModule();
builder.Services.AddPartialsModule();
builder.Services.AddAnalyticsModule();

// =============================================================================
// MVC & WEB
// =============================================================================

builder.Services.AddWebOptimizerConfig(builder.Environment);
builder.Services.AddMvcConfig();
builder.Services.AddSwapHtmxConfig();

// =============================================================================
// BUILD & RUN
// =============================================================================

var app = builder.Build();

await app.InitializeDatabaseAsync();
app.ConfigurePipeline();
app.MapEndpoints();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
