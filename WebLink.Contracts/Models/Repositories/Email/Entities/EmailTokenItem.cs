using System;

namespace WebLink.Contracts.Models
{
    public class EmailTokenItem : IEmailTokenItem
    {
        public int ID { get; set; }
        public int EmailTokenID { get; set; }
        public EmailToken EmailToken { get; set; }
        public int OrderID { get; set; }
        public bool Notified { get; set; }
        public DateTime? NotifyDate { get; set; }
        public bool Seen { get; set; }
        public DateTime? SeenDate { get; set; }
    }
}

