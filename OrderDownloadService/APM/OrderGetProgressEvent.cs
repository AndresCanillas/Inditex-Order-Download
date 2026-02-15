using Service.Contracts;

namespace OrderDonwLoadService
{
    public class OrderGetProgressEvent : EQEventInfo
    {
        public string OrderNumber { get; set; }
        public string StepId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
