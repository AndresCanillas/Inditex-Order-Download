using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebLink.Contracts.Models.Delivery;

namespace WebLink.Contracts.Models.Print.Configurations
{
    public class PackageDetailEntityConfiguration : IEntityTypeConfiguration<PackageDetail>
    {
        public void Configure(EntityTypeBuilder<PackageDetail> builder)
        {
           
        }
    }
}
