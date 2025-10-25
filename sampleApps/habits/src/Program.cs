using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using habits.Data;
using habits.Data.Backup;
using habits.Data.Models;
using habits.Data.Seed;
using habits.Services;
using habits.Services.Calendar;
using habits.Services.Documents;
using habits.Services.FileSystem;
using habits.Services.Notifications;
using habits.Services.Storage;
using habits.Services.Tasks;
using habits.Services.Users;
using habits.Settings;
using System.IO.Compression;
using Services.Calendar;
using habits.Services.LoyaltyCards;
using habits.Services.MealPlan;
using habits.Filters;

var builder = WebApplication.CreateBuilder(args);

var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString(isDocker ? "DefaultConnectionDocker" : "DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(365);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<CheckHxRequestAttribute>();
});
builder.Services.AddRazorPages();

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.Name = "Habits";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(365);
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
.AddCookie(options =>
{
    options.Cookie.MaxAge = TimeSpan.FromDays(365);
    options.ExpireTimeSpan = TimeSpan.FromDays(365);
    options.SlidingExpiration = true;
});

var keysPath = @"/app/keys";
if (!Directory.Exists(keysPath))
{
    Directory.CreateDirectory(keysPath);
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("Habits");

builder.Services.AddTransient<IEmailSender, GmailService>();
builder.Services.AddTransient<ITaskService, TaskService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IItemService, ItemService>();

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ILoyaltyCardService, LoyaltyCardService>();
builder.Services.AddScoped<ICalendarEventTypeService, CalendarEventTypeService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IMealPlanService, MealPlanService>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppUserClaimsPrincipalFactory>();

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
// Add FCM settings
builder.Services.Configure<FcmSettings>(builder.Configuration.GetSection("FcmSettings"));
builder.Services.AddScoped<IFcmNotificationService, FcmNotificationService>();

builder.Services.AddSession();

builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();
builder.Services.AddScoped<ICalendarNotificationProcessor, CalendarNotificationProcessor>();
builder.Services.AddHostedService<CalendarNotificationService>();

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

builder.Configuration.GetSection("FcmSettings").GetSection("ProjectId").Value = Environment.GetEnvironmentVariable("FIREBASE_ADMIN_PROJECT_ID");
builder.Configuration.GetSection("FcmSettings").GetSection("PrivateKeyId").Value = Environment.GetEnvironmentVariable("FIREBASE_ADMIN_PRIVATE_KEY_ID");
builder.Configuration.GetSection("FcmSettings").GetSection("PrivateKey").Value = Environment.GetEnvironmentVariable("FIREBASE_ADMIN_PRIVATE_KEY");
builder.Configuration.GetSection("FcmSettings").GetSection("ClientEmail").Value = Environment.GetEnvironmentVariable("FIREBASE_ADMIN_CLIENT_EMAIL");
builder.Configuration.GetSection("FcmSettings").GetSection("ClientX509CertUrl").Value = Environment.GetEnvironmentVariable("FIREBASE_CLINET_X509_CERT_URL");
builder.Configuration.GetSection("FcmSettings").GetSection("ClientId").Value = Environment.GetEnvironmentVariable("FIREBASE_ADMIN_CLIENT_ID");

// Firebase Client (Web) configuration
builder.Configuration["Firebase:ApiKey"] = Environment.GetEnvironmentVariable("FIREBASE_API_KEY");
builder.Configuration["Firebase:ProjectId"] = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
builder.Configuration["Firebase:MessagingSenderId"] = Environment.GetEnvironmentVariable("FIREBASE_MESSAGING_SENDER_ID");
builder.Configuration["Firebase:AppId"] = Environment.GetEnvironmentVariable("FIREBASE_APP_ID");
builder.Configuration["Firebase:VapidKey"] = Environment.GetEnvironmentVariable("FIREBASE_VAPID_KEY");

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 30 * 1024 * 1024;
});

var app = builder.Build();

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
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.UseSession();

app.UseMiddleware<ActiveUserMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Service-Worker-Allowed", "/");
    await next();
});

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/Home/Index");
        return;
    }

    await next();
});

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
    await DataSeeder.Seed(services);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
}

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        await next();
    });
}

app.Run();