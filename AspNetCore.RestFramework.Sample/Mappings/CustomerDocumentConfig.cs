using AspNetRestFramework.Sample.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetRestFramework.Sample.Mappings
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
