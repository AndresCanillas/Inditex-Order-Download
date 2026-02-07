using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class ERPConfig : IERPConfig
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
