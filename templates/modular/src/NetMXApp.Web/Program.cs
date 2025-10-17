using Identity.Application.Roles;
using Identity.Application.Users;
using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain.Repositories;
using NetMX.EntityFrameworkCore.Repositories;
using NetMXApp.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
        });
});

// Register repositories for Identity module
builder.Services.AddScoped<IQueryableRepository<AppUser, Guid>>(sp => 
    new EfCoreRepository<AppDbContext, AppUser, Guid>(sp.GetRequiredService<AppDbContext>()));
builder.Services.AddScoped<IQueryableRepository<AppRole, Guid>>(sp => 
    new EfCoreRepository<AppDbContext, AppRole, Guid>(sp.GetRequiredService<AppDbContext>()));
builder.Services.AddScoped<IQueryableRepository<AppUserRole, Guid>>(sp => 
    new EfCoreRepository<AppDbContext, AppUserRole, Guid>(sp.GetRequiredService<AppDbContext>()));

// Register Identity module application services
builder.Services.AddScoped<UserAppService>();
builder.Services.AddScoped<RoleAppService>();

var app = builder.Build();

// Apply pending migrations automatically in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // This will create the database if it doesn't exist and apply any pending migrations
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
