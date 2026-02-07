using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IOrderNotificationManager
    {
        /// <summary>
        /// Rerturn UserId List with Customers Assigned in Project or Company for contact and ProdManager For de Location
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        IEnumerable<string> GetIDTStakeholders(int projectId, int? locationId);
        void RegisterCancelledNotification(IOrder order);
        void RegisterErrorNotification(int? projectId, int? locationId, ErrorNotificationType errorType, string title, string message, string key, int? orderId);
        void RegisterResetValidationNotification(IOrder order);
        void RegisterReceivedNotification(IOrder order);
        void RegisterEmailNotificationForOrder(IOrder order, EmailType type);

    }
}
