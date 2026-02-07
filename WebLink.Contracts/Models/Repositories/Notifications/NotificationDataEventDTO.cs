using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class NotificationDataEventDTO
    {
        public Notification Notification { get; set; }
        public int CompanyID { get; set; }
        public int BrandID { get; set; }
        public int ProjectID { get; set; }
        public ErrorNotificationType ErrorType { get; set; }
        public string  Key {get;set;}
    }
}
