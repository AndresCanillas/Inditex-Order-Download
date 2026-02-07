using System;

namespace WebLink.Contracts.Models
{
    public class EmailTokenItemError : IEmailTokenItemError
    {
        public int ID { get; set; }
        public int EmailTokenID { get; set; }
        public EmailToken EmailToken { get; set; }
        public string TokenKey { get; set; }
        public ErrorNotificationType TokenType { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int? LocationID { get; set; }
        public int? ProjectID { get; set; }
        public bool Notified { get; set; }
        public DateTime? NotifyDate { get; set; }
        public bool Seen { get; set; }
        public DateTime? SeenDate { get; set; }
    }
}

