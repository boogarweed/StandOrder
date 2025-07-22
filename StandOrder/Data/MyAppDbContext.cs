using Microsoft.EntityFrameworkCore;
using StandOrder.Data;

namespace StandOrder.Data
{
    public partial class MyAppDbContext : DbContext
    {
        public DbSet<OrdersTaken> OrdersTaken { get; set; }
        public DbSet<OrdersTakenDetails> OrdersTakenDetails { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Barcode> Barcodes { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Keep this if it's already here

            // Add this configuration for each table that has a trigger
            modelBuilder.Entity<OrdersTakenDetails>()
                .ToTable(tb => tb.UseSqlOutputClause(false));
        }
        public MyAppDbContext(DbContextOptions<MyAppDbContext> options) : base(options)
        {
        }
    }
}
