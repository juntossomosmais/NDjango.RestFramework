using AspNetRestFramework.Sample.Mappings;
using AspNetRestFramework.Sample.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetRestFramework.Sample.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }


        public DbSet<Customer> Customer { get; set; }
        public DbSet<Seller> Seller { get; set; }
        public DbSet<CustomerDocument> CustomerDocument { get; set; }
        public DbSet<IntAsIdEntity> IntAsIdEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new CustomerConfig());
            modelBuilder.ApplyConfiguration(new SellerConfig());
            modelBuilder.ApplyConfiguration(new CustomerDocumentConfig());
            modelBuilder.ApplyConfiguration(new IntAsIdEntityConfig());
        }
    }
}
