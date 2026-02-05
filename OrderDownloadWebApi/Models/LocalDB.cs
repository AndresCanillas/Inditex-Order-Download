using Microsoft.EntityFrameworkCore;

namespace OrderDownloadWebApi.Models
{
    public class LocalDB : DbContext
    {
        public LocalDB(DbContextOptions<LocalDB> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            base.Database.SetCommandTimeout(10);

            builder.Entity<User>()
                    .HasIndex(u => u.Name)
                    .IsUnique();
        }

        public DbSet<User> Users { get; set; }


    }
}
