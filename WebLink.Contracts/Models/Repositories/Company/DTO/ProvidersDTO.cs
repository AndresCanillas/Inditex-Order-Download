using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{ 
    public class ProvidersDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string ClientReference { get; set; } // referencer from companyprovidertable
    }
}
