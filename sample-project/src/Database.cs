using Microsoft.EntityFrameworkCore;

namespace SampleProject;

public class AppDbContext : DbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<RestaurantProfile> RestaurantProfiles => Set<RestaurantProfile>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemIngredient> MenuItemIngredients => Set<MenuItemIngredient>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Gift> Gifts => Set<Gift>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Address).HasMaxLength(300);
            entity.Property(r => r.Phone).HasMaxLength(40);
        });

        modelBuilder.Entity<RestaurantProfile>(entity =>
        {
            entity.HasOne(p => p.Restaurant)
                .WithOne(r => r.Profile)
                .HasForeignKey<RestaurantProfile>(p => p.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(p => p.Website).HasMaxLength(200);
            entity.Property(p => p.OpeningHours).HasMaxLength(100);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasOne(m => m.Restaurant)
                .WithMany(r => r.MenuItems)
                .HasForeignKey(m => m.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(m => m.Name).IsRequired().HasMaxLength(150);
            entity.Property(m => m.Description).HasMaxLength(500);
            entity.Property(m => m.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(i => i.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(i => i.Name).IsUnique();
        });

        modelBuilder.Entity<MenuItemIngredient>(entity =>
        {
            entity.HasKey(mi => new { mi.MenuItemId, mi.IngredientId });
            entity.HasOne(mi => mi.MenuItem)
                .WithMany()
                .HasForeignKey(mi => mi.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(mi => mi.Ingredient)
                .WithMany()
                .HasForeignKey(mi => mi.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(t => t.Name).IsUnique();
            entity.Property(t => t.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(t => t.Slug).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.Action).IsRequired().HasMaxLength(50);
            entity.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Detail).HasMaxLength(2000);
        });

        modelBuilder.Entity<Gift>(entity =>
        {
            entity.Property(g => g.Name).IsRequired().HasMaxLength(150);
            entity.Property(g => g.Price).HasPrecision(12, 2);
            entity.Property(g => g.Description).HasMaxLength(500);
            entity.Property(g => g.Notes).HasMaxLength(500);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<StandardEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
