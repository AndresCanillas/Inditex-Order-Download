using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebLink.Contracts.Models.Delivery;

namespace WebLink.Contracts.Models.Print.Configurations
{
    public class DeliveryNoteEntityConfiguration : IEntityTypeConfiguration<DeliveryNote>
    {
        public void Configure(EntityTypeBuilder<DeliveryNote> builder)
        {
           
        }
    }
}
