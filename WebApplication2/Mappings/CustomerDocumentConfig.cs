using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApplication2.Models;

namespace WebApplication2.Mappings
{
    public class CustomerDocumentConfig : IEntityTypeConfiguration<CustomerDocument>
    {
        public void Configure(EntityTypeBuilder<CustomerDocument> builder)
        {
            builder.HasOne(b => b.Customer)
                .WithMany(b => b.CustomerDocuments)
                .HasForeignKey(b => b.CustomerId)
                .IsRequired();
        }
    }
}
