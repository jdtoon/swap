using Microsoft.EntityFrameworkCore;
using KanbanApp.Data;
using KanbanApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<KanbanDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application Services
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IListService, ListService>();
builder.Services.AddScoped<ICardService, CardService>();

// Session (for HTMX interactions)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseSession();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KanbanDbContext>();
    db.Database.Migrate();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Board}/{action=Index}/{id?}");

app.Run();
