using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WebLink.Contracts.Models.Configuration
{
    public class EncodeLabelEntityConfiguration : IEntityTypeConfiguration<EncodedLabel>
    {
        public void Configure(EntityTypeBuilder<EncodedLabel> builder)
        {
            builder.HasIndex(p => p.EPC);
            builder.HasIndex(p => p.CompanyID);
            builder.HasIndex(p => p.ProjectID);
            builder.HasIndex(p => p.OrderID);
            builder.HasIndex(p => p.Date);
        }
    }
}
