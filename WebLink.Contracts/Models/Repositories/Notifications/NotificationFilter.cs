using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public enum NotificationFilterSortDirection
    {
        Default = 0,
        NewFirst,
        OldFirst
    }

    public class NotificationFilter
    {
        public NotificationType Type { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? LocationID { get; set; }
        public int? CompanyID { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public NotificationFilterSortDirection SortDirection { get; set; }

        public NotificationFilter()
        {
            CurrentPage = 1;
            PageSize = 20;
            From = DateTime.Now.AddDays(-15);
            Type = NotificationType.All;
            SortDirection = NotificationFilterSortDirection.NewFirst;
        }

    }
}
