using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ttw.Data;
using ttw.Data.Backup;
using ttw.Data.Models;
using ttw.Data.Seed;
using ttw.Services;
using ttw.Services.Storage;
using ttw.Settings;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc.ViewEngines;

var builder = WebApplication.CreateBuilder(args);

var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString(isDocker ? "DefaultConnectionDocker" : "DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<AppUser, AppRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); ;

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IViewEngine, CompositeViewEngine>();
builder.Services.AddSingleton<BrowserService>();

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(180);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.IsEssential = true;
    
    // Add login path configuration
    options.LoginPath = "/Identity/Login";
    options.LogoutPath = "/Identity/Logout";
    options.AccessDeniedPath = "/Identity/AccessDenied";
});

var keysPath = @"/app/keys";
if (!Directory.Exists(keysPath))
{
    Directory.CreateDirectory(keysPath);
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("TTW");

builder.Services.AddTransient<IEmailSender, GmailService>();
builder.Services.AddSingleton<IR2StorageService, R2StorageService>();

// Only add DatabaseBackupService in non-development environments
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.SmallestSize;
    });

    builder.Services.AddHostedService<DatabaseBackupService>();
}

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<CloudflareR2Settings>(builder.Configuration.GetSection("CloudflareR2Settings"));

builder.Services.AddSession();

// Add after builder configuration
if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load(); // This loads the .env file from the project root
}

// Add configuration sources
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Map environment variables to configuration
builder.Configuration.GetSection("SmtpSettings").GetSection("Server").Value = Environment.GetEnvironmentVariable("SMTP_SERVER");
builder.Configuration.GetSection("SmtpSettings").GetSection("Port").Value = Environment.GetEnvironmentVariable("SMTP_PORT");
builder.Configuration.GetSection("SmtpSettings").GetSection("SenderName").Value = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME");
builder.Configuration.GetSection("SmtpSettings").GetSection("SenderEmail").Value = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL");
builder.Configuration.GetSection("SmtpSettings").GetSection("Username").Value = Environment.GetEnvironmentVariable("SMTP_USERNAME");
builder.Configuration.GetSection("SmtpSettings").GetSection("Password").Value = Environment.GetEnvironmentVariable("SMTP_PASSWORD");

builder.Configuration.GetSection("CloudflareR2Settings").GetSection("AccessKey").Value = Environment.GetEnvironmentVariable("CLOUDFLARE_R2_ACCESS_KEY");
builder.Configuration.GetSection("CloudflareR2Settings").GetSection("SecretKey").Value = Environment.GetEnvironmentVariable("CLOUDFLARE_R2_SECRET_KEY");
builder.Configuration.GetSection("CloudflareR2Settings").GetSection("BucketName").Value = Environment.GetEnvironmentVariable("CLOUDFLARE_R2_BUCKET_NAME");
builder.Configuration.GetSection("CloudflareR2Settings").GetSection("Endpoint").Value = Environment.GetEnvironmentVariable("CLOUDFLARE_R2_ENDPOINT");
builder.Configuration.GetSection("CloudflareR2Settings").GetSection("BackupPrefix").Value = Environment.GetEnvironmentVariable("CLOUDFLARE_R2_BACKUP_PREFIX");

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 30 * 1024 * 1024;
});

var app = builder.Build();

// Add these lines before app.UseHttpsRedirection()
app.UseStaticFiles(); // Enable serving static files
app.UseDefaultFiles(); // Allows serving default files like index.html

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseResponseCompression();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.UseSession();

app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var services = scope.ServiceProvider;

try
{
    var rawConnectionString = builder.Configuration.GetConnectionString(isDocker ? "DefaultConnectionDocker" : "DefaultConnection");

    var connectionStringBuilder = new SqliteConnectionStringBuilder(rawConnectionString);
    var dbFilePath = connectionStringBuilder.DataSource;

    var dbDirectory = Path.GetDirectoryName(dbFilePath);
    if (!Directory.Exists(dbDirectory))
    {
        Directory.CreateDirectory(dbDirectory!);
    }

    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(services);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
}

app.Run();