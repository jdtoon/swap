using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ttw.Data.Models;

namespace ttw.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<City> City { get; set; }
        public DbSet<Currency> Currency { get; set; }
        public DbSet<Hotel> Hotel { get; set; }
        public DbSet<RateCard> RateCard { get; set; }
        public DbSet<RoomType> RoomType { get; set; }
        public DbSet<Supplier> Supplier { get; set; }
    }
}