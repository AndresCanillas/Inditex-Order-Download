using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models.Configuration
{
    public class OrderUpdatePropertiesEntityConfiguration : IEntityTypeConfiguration<OrderUpdateProperties>
    {
        public void Configure(EntityTypeBuilder<OrderUpdateProperties> builder)
        {
            builder.HasIndex(p => p.IsActive);
            builder.HasIndex(p => p.IsRejected);
        }
    }
}
