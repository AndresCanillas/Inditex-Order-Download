using System;

namespace WebLink.Contracts.Models
{
    public class JomaSerialSequence
    {
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public int NextValue { get; set; }
    }
}

