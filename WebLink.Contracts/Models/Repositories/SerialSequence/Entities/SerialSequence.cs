using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public class SerialSequence
    {
        [MaxLength(40)]
        public string ID { get; set; }
        [MaxLength(100)]
        public string Filter { get; set; }
        public long NextValue { get; set; }
    }
}

