using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebLink.Contracts.Models.Delivery;

namespace WebLink.Contracts.Models.Print.Configurations
{
    public class PackageEntityConfiguration : IEntityTypeConfiguration<Package>
    {
        void IEntityTypeConfiguration<Package>.Configure(EntityTypeBuilder<Package> builder)
        {
           
        }
    }
}
