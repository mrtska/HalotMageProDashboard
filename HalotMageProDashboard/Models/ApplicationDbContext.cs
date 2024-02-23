using HalotMageProDashboard.Entities;
using Microsoft.EntityFrameworkCore;

namespace HalotMageProDashboard.Models {
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options) {

        public DbSet<SavedPrinter> SavedPrinters { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            builder.Entity<SavedPrinter>().ToTable(nameof(SavedPrinter));
        }

    }
}
