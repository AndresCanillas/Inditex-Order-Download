using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models.Configuration
{
    public class CompanyProviderEntityConfiguration : IEntityTypeConfiguration<CompanyProvider>
    {
        public void Configure(EntityTypeBuilder<CompanyProvider> builder)
        {
            builder.HasMany(p => p.Orders)
                .WithOne(p => p.Provider)
                .HasForeignKey("ProviderRecordID")
                .HasPrincipalKey("ID");

        }
    }
}
