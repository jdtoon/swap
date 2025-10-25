using EcomApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApp.Data;

public class EcomDbContext : DbContext
{
    public EcomDbContext(DbContextOptions<EcomDbContext> options) : base(options)
    {
    }
    
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasOne(e => e.Category)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId);
        });
        
        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });
        
        // Cart
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.SessionId);
        });
        
        // CartItem
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceAtAdd).HasPrecision(18, 2);
            entity.HasOne(e => e.Cart)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.CartId);
            entity.HasOne(e => e.Product)
                .WithMany(e => e.CartItems)
                .HasForeignKey(e => e.ProductId);
        });
        
        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
        });
        
        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceAtOrder).HasPrecision(18, 2);
            entity.HasOne(e => e.Order)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.OrderId);
            entity.HasOne(e => e.Product)
                .WithMany(e => e.OrderItems)
                .HasForeignKey(e => e.ProductId);
        });
        
        // Seed Data
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and gadgets" },
            new Category { Id = 2, Name = "Books", Description = "Books and reading materials" },
            new Category { Id = 3, Name = "Clothing", Description = "Clothing and accessories" }
        );
        
        // Products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Stock = 10, CategoryId = 1 },
            new Product { Id = 2, Name = "Smartphone", Description = "Latest smartphone", Price = 699.99m, Stock = 25, CategoryId = 1 },
            new Product { Id = 3, Name = "Headphones", Description = "Wireless headphones", Price = 149.99m, Stock = 50, CategoryId = 1 },
            new Product { Id = 4, Name = "C# Programming", Description = "Learn C# the right way", Price = 39.99m, Stock = 100, CategoryId = 2 },
            new Product { Id = 5, Name = "Design Patterns", Description = "Gang of Four patterns", Price = 49.99m, Stock = 75, CategoryId = 2 },
            new Product { Id = 6, Name = "T-Shirt", Description = "Cotton t-shirt", Price = 19.99m, Stock = 200, CategoryId = 3 },
            new Product { Id = 7, Name = "Jeans", Description = "Blue denim jeans", Price = 59.99m, Stock = 100, CategoryId = 3 }
        );
    }
}
