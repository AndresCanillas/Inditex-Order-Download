using Service.Contracts;

namespace OrderDonwLoadService
{
    public class NotificationReceivedEvent : EQEventInfo
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string JsonData { get; set; }
        public string FileName { get;  set; }
    }
}