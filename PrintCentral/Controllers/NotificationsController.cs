using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
	[Authorize]
	public class NotificationsController : Controller
	{
		private INotificationRepository repo;
		private ILocalizationService g;
		private ILogService log;

		public NotificationsController(
			INotificationRepository repo,
			ILocalizationService g,
			ILogService log)
		{
			this.repo = repo;
			this.g = g;
			this.log = log;
		}

		[HttpPost, Route("/notifications/dismiss/{id}")]
		public OperationResult Dismiss(int id)
		{
			try
			{
				repo.Dismiss(id);
				return new OperationResult(true, g["Notification dismissed!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				if (ex.IsNameIndexException())
					return new OperationResult(false, g["There is already an item with that name."]);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}


		[HttpGet, Route("/notifications/getcount")]
		public int GetNotificationCount()
		{
			try
			{
				return repo.CountByFilter(new NotificationFilter() { 
					From = DateTime.Now.AddDays(-90),
					To = DateTime.Now,
					PageSize = 100
				});
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return -1;
			}
		}


		[HttpGet, Route("/notifications/getbyid/{id}")]
		public INotification GetByID(int id)
		{
			try
			{
				return repo.GetByID(id);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpGet, Route("/notifications/getlist")]
		public List<INotification> GetList()
		{
			try
			{
				return repo.GetList();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpGet, Route("/notifications/getrecent")]
		public List<INotification> GetRecent()
		{
			try
			{
				return repo.GetRecentNotifications();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpPost, Route("/notifications/getbyfilter")]
		public OperationResult GetByFilter([FromBody] NotificationFilter filter)
		{
			try
			{
				return new OperationResult() { Success = true, Data = new { Records = repo.GetByFilter(filter), Filter = filter } };
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult() { Success = false};
			}
		}
	}
}