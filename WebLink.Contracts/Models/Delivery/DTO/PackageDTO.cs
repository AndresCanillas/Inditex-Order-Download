using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models.Delivery.DTO
{
    public class PackageDTO
    {
        public Package Package { get; set; }
        public List<PackageDetail> Details { get; set; }
    }
}
