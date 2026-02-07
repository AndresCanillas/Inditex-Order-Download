using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class Notification : INotification, ICompanyFilter<Notification>, ISortableSet<Notification>
    {

        public const char ROLE_SEPARATOR = '|';

        public int ID { get; set; }
        public int CompanyID { get; set; }              // The company to which this notification is directed to (users from other companies will not see it). NOTE: SysAdmin users should automatically have access to all notifications.
        public Company Company { get; set; }
        public NotificationType Type { get; set; }
        [MaxLength(30)]
        public string IntendedRole { get; set; }
        [MaxLength(30)]
        public string IntendedUser { get; set; }
        [MaxLength(120)]
        public string NKey { get; set; }                // Unique key identifying this notification, if a notification with this same key exists then instead of adding a new notification the existing notification will be updated and its count field increased. This prevents the system from being saturated with repeated notifications.
        [MaxLength(256)]
        public string Source { get; set; }              // This field is inteded just for user reference, it can help determine from where in the system was the notification generated.
        [MaxLength(256)]
        public string Title { get; set; }               // Title of the notification.
        public string Message { get; set; }             // The main notification message (content)
        public string Data { get; set; }                // Generic data associated with the notification, must contain an object serialized as json. The meaning of this object depends on the type of notification being generated.
        public bool AutoDismiss { get; set; }           // True = The notification will be automatically deleted once the user interacts with it. False = The user will have to manually click the dismiss button to get rid of the notification.
        public int Count { get; set; }                  // Indicates how many times this notification (with same NKey) has been registered by the system.
        public string Action { get; set; }              // Specifies the action (notification controller) that should be used to display this notification. If null, then the default notification controller will be used. Custom notification controllers can be built to process specific notifications according to its intended purpose.
        public int? LocationID { get; set; }            // categorize notification by factory
        public int? ProjectID { get; set; }             // categoriza notification by project
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public int GetCompanyID(PrintDB db) => CompanyID;

        public Task<int> GetCompanyIDAsync(PrintDB db) => Task.FromResult(CompanyID);

        public IQueryable<Notification> FilterByCompanyID(PrintDB db, int companyid) =>
            from n in db.Notifications
            where n.CompanyID == companyid
            select n;

        public IQueryable<Notification> ApplySort(IQueryable<Notification> qry) => qry.OrderByDescending(p => p.UpdatedDate);

        public void SetData(object data)
        {
            Data = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        }

        public string GetData()
        {
            return Data;
        }
    }
}

