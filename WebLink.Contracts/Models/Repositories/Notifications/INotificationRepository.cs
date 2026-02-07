using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface INotificationRepository : IGenericRepository<INotification>
    {
        int GetNotificationCount();
        int GetNotificationCount(PrintDB ctx);

        List<INotification> GetByType(NotificationType type);
        List<INotification> GetByType(PrintDB ctx, NotificationType type);

        List<NotificationReportDTO> GetByFilter(NotificationFilter filer);
        List<NotificationReportDTO> GetByFilter(PrintDB ctx, NotificationFilter filer);

        List<INotification> GetRecentNotifications();
        List<INotification> GetRecentNotifications(PrintDB ctx);

        void Dismiss(int id);
        void Dismiss(PrintDB ctx, int id);

        void DismissKey(string key);
        void DismissKey(PrintDB ctx, string key);

        void AddNotification(int companyid, NotificationType type, string intendedRoles, string intendedUser, string nkey, string source, string title, string message, object data, bool autoDismiss, int? locationID, int? projectID, string actionController = null);
        void AddNotification(PrintDB ctx, int companyid, NotificationType type, string intendedRoles, string intendedUser, string nkey, string source, string title, string message, object data, bool autoDismiss, int? locationID, int? projectID, string actionController = null);
        int CountByFilter(NotificationFilter filter);
        int CountByFilter(PrintDB ctx, NotificationFilter filter);

        void SendIDTErrorEmail(string key, EmailType emailType, ErrorNotificationType errorType, int projectid, int? productionLocation, string title, string message, int? orderid);
        void SendErrorEmail(string key, string title, string message, IEnumerable<string> stakeHolders, EmailType emailType, ErrorNotificationType errorType, int projectid, int companyID, int? productionLocation, int? orderid);

        IEnumerable<string> GetIDTStakeholders(int projectId, int? locationId);
        IEnumerable<string> GetIDTStakeholders(PrintDB ctx, int projectId, int? locationId);

    }
}
