using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Authentication;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	// ================================================================================================
	// When the automated process manager catches an exception from any of the processes or event
	// handlers it invokes, it logs the exception and then raises an error event (APMErrorNotification).
	// We can subscribe to that notification, in this case to push a message through the
	// INotificationRepository.
	// ================================================================================================
	public class CreateErrorNotification : EQEventHandler<APMErrorNotification>
	{
		private ILogService log;
		private INotificationRepository notificationRepo;
		private IOrderEmailService mailService;
		private IProjectRepository projectRepo;
		private IUserRepository userRepo;
		private IOrderRepository orderRepo;

		public CreateErrorNotification(ILogService log, INotificationRepository notificationRepo, IOrderEmailService mailService, IProjectRepository projectRepo, IUserRepository userRepo, IOrderRepository orderRepo)
		{
			this.log = log;
			this.notificationRepo = notificationRepo;
			this.mailService = mailService;
			this.projectRepo = projectRepo;
			this.userRepo = userRepo;
			this.orderRepo = orderRepo;
		}

		public override EQEventHandlerResult HandleEvent(APMErrorNotification e)
		{
			// Create a notification for the error captured by the automated process manager
			try
			{
				RegisterNotification(e);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
			return EQEventHandlerResult.OK;
		}

		private void RegisterNotification(APMErrorNotification e)
		{
			if (string.IsNullOrEmpty(e.Type))
			{
				notificationRepo.AddNotification(
					companyid: 1, 
					type: NotificationType.OrderTracking,
					intendedRoles: Roles.SysAdmin,
					intendedUser: null,
					nkey: e.NotificationKey,
					source: e.HandlerType,
					title: "Automated Process Manager Error",
					message: e.Message,
					data: new { Error = e.Message, StackTrace = e.StackTrace, EventData = e.Data },
					autoDismiss: true,
					locationID: null,
					projectID: null
					);

				var orderId = ((JObject)e.Data).GetValue<int>("OrderID", 0);

				if (orderId > 0)
				{
					var orderInfo = orderRepo.GetProjectInfo(orderId);
					var key = $"OrderID-{orderInfo.OrderID}";
					RegisterCustomerEmail(orderInfo.ProjectID, orderInfo.LocationID, ErrorNotificationType.SystemError, "Automated Process Manager Error", e.Message, key,  orderId);
				}
			}
			else
			{
				// para este caso Data es una NotificationDataEventDTO, pero viene serializado en un JObject
				NotificationDataEventDTO dto = (NotificationDataEventDTO)((JObject)e.Data).ToObject(typeof(NotificationDataEventDTO));
				var data = dto.Notification;

				notificationRepo.AddNotification(
					companyid: data.CompanyID,
					type: data.Type,
					intendedRoles: data.IntendedRole,
					intendedUser: data.IntendedUser,
					nkey: e.NotificationKey,
					source: data.Source,
					title: data.Title,
					message: data.Message,
					data: new { Error = e.Message, StackTrace = e.StackTrace, dto.CompanyID, dto.ProjectID, dto.BrandID },
					autoDismiss: false,
					locationID: data.LocationID,
					projectID: data.ProjectID,
					actionController: "SystemErrorView");

				RegisterCustomerEmail(e);
			}
		}

		private void RegisterCustomerEmail(APMErrorNotification e)
		{
			NotificationDataEventDTO dto = (NotificationDataEventDTO)((JObject)e.Data).ToObject(typeof(NotificationDataEventDTO));
			var data = dto.Notification;

			RegisterCustomerEmail(data.ProjectID, data.LocationID, dto.ErrorType, dto.Notification.Title, dto.Notification.Message, dto.Key, null);
			
		}

		private void RegisterCustomerEmail(int? projectId, int? locationId, ErrorNotificationType errorType, string title, string message, string key, int? orderId)
		{
			var projectIdValue = projectId.HasValue ? projectId.Value : 1;

			var recipients = GetStakeholders(projectIdValue, locationId);

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

		private IEnumerable<string> GetStakeholders(int projectId, int? locationId)
		{
			// get customer service persont for this project or company
			var recipients = projectRepo.GetCustomerEmails(projectId);

			var prodManagers = userRepo.GetProdManagers(locationId)
				.Select(s => s.Id);

			recipients.AddRange(prodManagers);

			return recipients;
		}
	}
}
