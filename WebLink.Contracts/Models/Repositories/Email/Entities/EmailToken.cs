namespace WebLink.Contracts.Models
{
    public class EmailToken : IEmailToken
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string UserId { get; set; }
        public EmailType Type { get; set; }
    }
}

