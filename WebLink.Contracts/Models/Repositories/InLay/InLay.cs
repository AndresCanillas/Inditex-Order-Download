using System;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public class InLay : IInLay
    {
        public int ID { get; set; }
        public string ChipName { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string ProviderName { get; set; }
        public string Model { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }        
    }
}
