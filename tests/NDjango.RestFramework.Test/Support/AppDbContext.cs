using Microsoft.EntityFrameworkCore;

namespace NDjango.RestFramework.Test.Support;

public class AppDbContext : DbContext
{
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Seller> Seller { get; set; }
    public DbSet<CustomerDocument> CustomerDocument { get; set; }
    public DbSet<IntAsIdEntity> IntAsIdEntities { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity => { entity.HasKey(b => b.Id); });
        modelBuilder.Entity<Seller>(entity => { entity.HasKey(b => b.Id); });
        modelBuilder.Entity<CustomerDocument>(entity =>
        {
            entity.HasOne(b => b.Customer)
                .WithMany(b => b.CustomerDocument)
                .HasForeignKey(b => b.CustomerId)
                .IsRequired();
        });
        modelBuilder.Entity<IntAsIdEntity>(entity => { entity.HasKey(b => b.Id); });
    }
}
