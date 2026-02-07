using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class NotificationReportDTO
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public NotificationType Type { get; set; }
        public string IntendedRole { get; set; }
        public string IntendedUser { get; set; }
        public string NKey { get; set; }
        public string Source { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public bool AutoDismiss { get; set; }
        public int Count { get; set; }
        public string Action { get; set; }
        public int? LocationID { get; set; }
        public int? ProjectID { get; set; }
        public string CompanyName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string LocationName { get; set; }
        public string LocationCode { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
