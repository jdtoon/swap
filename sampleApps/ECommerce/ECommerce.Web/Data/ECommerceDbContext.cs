using ECommerce.Web.Models;
using Microsoft.EntityFrameworkCore;
using NetMX.EntityFrameworkCore;

namespace ECommerce.Web.Data;

public class ECommerceDbContext : NetMXDbContext<ECommerceDbContext>
{
    public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }









}
