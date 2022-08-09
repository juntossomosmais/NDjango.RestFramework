using AspNetRestFramework.Sample.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetRestFramework.Sample.Mappings
{
    public class IntAsIdEntityConfig : IEntityTypeConfiguration<IntAsIdEntity>
    {
        public void Configure(EntityTypeBuilder<IntAsIdEntity> builder)
        {
            builder.HasKey(b => b.Id);
        }
    }
}
