namespace WebLink.Contracts.Models
{
    public class EmailServiceSettings : IEmailServiceSettings
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public bool NotifyOrderReceived { get; set; } = true;
        public bool NotifyOrderPendingValidation { get; set; } = true;
        public bool NotifyOrderValidated { get; set; } = true;
        public bool NotifyOrderConflict { get; set; } = true;
        public bool NotifyOrderReadyForProduction { get; set; } = true;
        public bool NotifyOrderCompleted { get; set; } = true;
        public int NotificationPeriodInDays { get; set; } = 1;
        public bool NotifyOrderProcesingErrors { get; set; } = true; // only for IDT
        public bool NotifyOrderCancelled { get; set; } = true;
        public bool NotifyOrderPoolUpdate { get; set; } = true;
    }
}
