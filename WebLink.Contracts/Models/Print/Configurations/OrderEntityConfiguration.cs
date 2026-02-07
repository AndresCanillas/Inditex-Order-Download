using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WebLink.Contracts.Models.Configuration
{
    public class OrderEntityConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasIndex(o => o.OrderStatus);
            builder.HasIndex(o => o.ProviderRecordID);
            builder.HasIndex(o => o.SendToCompanyID);
            builder.HasIndex(o => o.IsInConflict);
            builder.HasIndex(o => o.IsStopped);
            builder.HasIndex(o => o.IsBilled);
            builder.HasIndex(o => o.ProductionType);
            builder.HasIndex(o => o.CreatedDate);
            builder.HasIndex(o => o.UpdatedDate);


        }
    }
}
