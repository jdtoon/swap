using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetMX.Identity.Core.Data;
using NetMX.Identity.Core.Users;
using NetMXApp.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Entity Framework Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
    });
});

// Configure Identity DbContext
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Identity");
        npgsqlOptions.MigrationsAssembly("NetMXApp.Web"); // Store migrations in Web project
    });
});

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
});

var app = builder.Build();

// Apply pending migrations automatically in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    
    // Migrate App DbContext
    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await appDbContext.Database.MigrateAsync();
    
    // Migrate Identity DbContext
    var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await identityDbContext.Database.MigrateAsync();
    
    // Seed admin user
    await SeedDataAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Add authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Data seeding method
static async Task SeedDataAsync(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
    
    // Create Admin role
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        var adminRole = new AppRole(Guid.NewGuid(), "Admin", "Administrator role", isSystemRole: true);
        await roleManager.CreateAsync(adminRole);
    }
    
    // Create User role
    if (!await roleManager.RoleExistsAsync("User"))
    {
        var userRole = new AppRole(Guid.NewGuid(), "User", "Standard user role", isSystemRole: false);
        await roleManager.CreateAsync(userRole);
    }
    
    // Create admin user
    var adminEmail = "admin@netmx.dev";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        // Create user instance using Activator to bypass constructor validation
        // UserManager will set the password hash when we call CreateAsync
        adminUser = (AppUser)Activator.CreateInstance(typeof(AppUser), true)!;
        adminUser.Id = Guid.NewGuid();
        adminUser.UserName = "admin";
        adminUser.Email = adminEmail;
        adminUser.EmailConfirmed = true;
        
        // Set profile information
        adminUser.UpdateProfile("System", "Administrator", null);
        
        // Create with password - UserManager will hash it
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine($"✓ Created admin user: {adminEmail} / Admin123!");
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            Console.WriteLine($"✗ Failed to create admin user: {errors}");
        }
    }
}
