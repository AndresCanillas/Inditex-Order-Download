using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    public class OrderNotificationManager : IOrderNotificationManager
    {
        private IOrderEmailService mailService;
        private IProjectRepository projectRepo;
        private IUserRepository userRepo;

        public OrderNotificationManager(
            IOrderEmailService mailService,
            IProjectRepository projectRepo,
            IUserRepository userRepo

            )
        {
            this.mailService = mailService;
            this.projectRepo = projectRepo;
            this.userRepo = userRepo;
        }

        


        public void RegisterErrorNotification(int? projectId, int? locationId, ErrorNotificationType errorType, string title, string message, string key, int? orderId)
        {
            var projectIdValue = projectId.HasValue ? projectId.Value : 1;

            var recipients = GetIDTStakeholders(projectIdValue, locationId);

            foreach (var usr in recipients)
            {
                var token = mailService.GetTokenFromUser(usr, EmailType.OrderProcessingError);
                if (token != null)
                    mailService.AddErrorIfNotExist(token: token,
                        errorType: errorType,
                        title: title,
                        message: message,
                        key: key,
                        projectId: projectId,
                        locationId: locationId, // report order IDT location
                        orderId: orderId
                        );
            }
        }

        /// <summary>
        /// Notify by Email:
        /// - IDT Customer Service
        /// - IDT Prod Managers
        /// - Client
        /// - Provider
        /// </summary>
        /// <param name="order"></param>
        public void RegisterResetValidationNotification(IOrder order)
        {
            RegisterEmailNotificationForOrder(order, EmailType.OrderResetValidation);
        }

        /// <summary>
        /// Notify by Email:
        /// - IDT Customer Service
        /// - IDT Prod Managers
        /// - Client
        /// - Provider
        /// </summary>
        /// <param name="order"></param>
        public void RegisterCancelledNotification(IOrder order)
        {
            RegisterEmailNotificationForOrder(order, EmailType.OrderCancelled);
        }

        /// <summary>
        /// Notify by Email:
        /// - IDT Customer Service
        /// - IDT Prod Managers
        /// - Client
        /// - Provider
        /// </summary>
        /// <param name="order"></param>
        public void RegisterReceivedNotification(IOrder order)
        {
            RegisterEmailNotificationForOrder(order, EmailType.OrderReceived);
        }
            

        /// <summary>
        /// Notify by Email:
        /// - IDT Customer Service
        /// - IDT Prod Managers
        /// - Client
        /// - Provider
        /// </summary>
        /// <param name="order"></param>
        public void RegisterEmailNotificationForOrder(IOrder order, EmailType type)
        {
            var projectIdValue = order.ProjectID;

            var recipients = GetIDTStakeholders(projectIdValue, order.LocationID).ToList();

            var providersRecipient = mailService.GetProvidersUsersByOrder(order.ID);

            var clientRecipient = mailService.GetClientUsersByProject(order.ProjectID);

            recipients.AddRange(providersRecipient);
            recipients.AddRange(clientRecipient);

            foreach (var usr in recipients)
            {
                var token = mailService.GetTokenFromUser(usr, type);
                if (token != null)
                    mailService.AddOrderIfNotExists(token, order.ID);
            }

        }


        /// <summary>
        /// Rerturn UserId List with Customers Assigned in Project or Company as a Customer Contact and ProdManager For de Location
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public IEnumerable<string> GetIDTStakeholders(int projectId, int? locationId)
        {
            var recipients = projectRepo.GetCustomerEmails(projectId);

            var prodManagers = userRepo.GetProdManagers(locationId)
                .Select(s => s.Id);

            recipients.AddRange(prodManagers);

            return recipients.Distinct();
        }
        
        
    }
}
