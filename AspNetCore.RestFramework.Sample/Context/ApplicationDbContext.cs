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


        public Customer Customer { get; set; }
        public Seller Seller { get; set; }
        public CustomerDocument CustomerDocument { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new CustomerConfig());
            modelBuilder.ApplyConfiguration(new SellerConfig());
            modelBuilder.ApplyConfiguration(new CustomerDocumentConfig());
        }
    }
}
