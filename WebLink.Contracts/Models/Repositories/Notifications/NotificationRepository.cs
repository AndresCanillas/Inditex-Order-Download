using LinqKit;
using LinqKit.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Contracts.Models
{
	public class NotificationRepository: GenericRepository<INotification, Notification>, INotificationRepository
	{
		public NotificationRepository(IFactory factory)
			: base(factory, (ctx) => ctx.Notifications)
		{
            TriggerEntityEvents = false;
		}

		protected override string TableName { get => "Notifications"; }

		protected override void UpdateEntity(PrintDB ctx, IUserData userData, Notification actual, INotification data)
		{
			actual.Type = data.Type;
			actual.Source = data.Source;
			actual.NKey = data.NKey;
			actual.Message = data.Message;
			actual.Data = data.Data;
			actual.AutoDismiss = data.AutoDismiss;
			actual.Count = data.Count;
		}


		public override INotification Create()
		{
			return new Notification() { Count = 1 };
		}


		public override INotification Insert(PrintDB ctx, INotification data)
		{
			var existing = ctx.Notifications.Where(p => p.NKey == data.NKey).SingleOrDefault();
			if (existing != null)
			{
				existing.Count++;
				return base.Update(existing);
			}
			else return base.Insert(ctx, data);
		}

		public override IQueryable<INotification> All(PrintDB ctx)
		{
			var userData = factory.GetInstance<IUserData>();
			var userRoles = userData.UserRoles;

            var owner = NotificationRepository.BelongTo(userData);

            var q = base.All(ctx).Where(owner).OrderByDescending(p => p.UpdatedDate);
            
            return q;
		}

		public int GetNotificationCount()
		{
			using(var ctx = factory.GetInstance<PrintDB>())
			{
				return GetNotificationCount(ctx);
			}
		}

		public int GetNotificationCount(PrintDB ctx)
		{
			return All(ctx).Count();
		}

		public List<INotification> GetRecentNotifications()
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetRecentNotifications(ctx);
			}
		}

		public List<INotification> GetRecentNotifications(PrintDB ctx)
		{
			return new List<INotification>(All(ctx).OrderByDescending(n=>n.UpdatedDate).Take(20));
		}

		public List<INotification> GetByType(NotificationType type)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByType(ctx, type);
			}
		}

		public List<INotification> GetByType(PrintDB ctx, NotificationType type)
		{
			return new List<INotification>(
				All(ctx).Where(p => (type == NotificationType.All || p.Type == type))
			);
		}

		public List<NotificationReportDTO> GetByFilter(NotificationFilter filter)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByFilter(ctx, filter);
			}
		}

        public List<NotificationReportDTO> GetByFilter(PrintDB ctx, NotificationFilter filter)
		{
			// No use notificationRepository.all method
			var userData = factory.GetInstance<IUserData>();
			//var userRoles = userData.UserRoles;

            var q = from nt in ctx.Notifications
                     join locmap in ctx.Locations on nt.LocationID equals locmap.ID into Location
                     from loc in Location.DefaultIfEmpty()

                     join prjmap in ctx.Projects on nt.ProjectID equals prjmap.ID into Project
                     from prj in Project.DefaultIfEmpty()

                     join brandmap in ctx.Brands on prj.BrandID equals brandmap.ID into Brand
                     from brd in Brand.DefaultIfEmpty()

                     join cmpmap in ctx.Companies on brd.CompanyID equals cmpmap.ID into Company
                     from cmp in Company.DefaultIfEmpty()

                     where
                     nt.CreatedDate >= filter.From
                     && nt.CreatedDate <= filter.To
                     //&& (userData.IsSysAdmin || (userRoles.Contains(nt.IntendedRole)  || nt.IntendedUser == userData.Principal.Identity.Name))
                     && (filter.Type == NotificationType.All || nt.Type == filter.Type)
                     && (!filter.LocationID.HasValue || filter.LocationID == 0 || nt.LocationID == filter.LocationID)
                     && (!filter.CompanyID.HasValue || filter.CompanyID == 0 || cmp.ID == filter.CompanyID)

                     select new NotificationReportDTO
                     {
                         ID = nt.ID,
                         Type = nt.Type,
                         Source = nt.Source,
                         Title = nt.Title,
                         Message = nt.Message,
                         Data = nt.Data,
                         Action = nt.Action,
                         AutoDismiss = nt.AutoDismiss,
                         Count = nt.Count,
                         CompanyName = cmp.Name,
                         LocationID = nt.LocationID,
                         LocationName = loc.Name,
                         LocationCode = loc.FactoryCode,
                         CreatedDate = nt.CreatedDate,
                         UpdatedDate = nt.UpdatedDate,
                         IntendedRole = nt.IntendedRole,
                         IntendedUser = nt.IntendedUser
                     };

            var owner = NotificationRepository.BelongToInReport(userData);
            q = q.Where(owner);


            if (filter.SortDirection == NotificationFilterSortDirection.OldFirst)
			{
				q = q.OrderBy(n => n.CreatedDate);
			}
			else
			{
				q = q.OrderByDescending(n => n.UpdatedDate);
			}

			filter.TotalRecords = q.Count();

			return q.Skip((filter.CurrentPage - 1) * filter.PageSize).Take(filter.PageSize).ToList();
		}


		public int CountByFilter(NotificationFilter filter)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return CountByFilter(ctx, filter);
			}
		}

		public int CountByFilter(PrintDB ctx, NotificationFilter filter)
		{
			var userData = factory.GetInstance<IUserData>();
			var userRoles = userData.UserRoles;

			var q = from nt in ctx.Notifications
					//join locmap in ctx.Locations on nt.LocationID equals locmap.ID into Location
					//from loc in Location.DefaultIfEmpty()

					//join prjmap in ctx.Projects on nt.ProjectID equals prjmap.ID into Project
					//from prj in Project.DefaultIfEmpty()

					//join brandmap in ctx.Brands on prj.BrandID equals brandmap.ID into Brand
					//from brd in Brand.DefaultIfEmpty()

					//join cmpmap in ctx.Companies on brd.CompanyID equals cmpmap.ID into Company
					//from cmp in Company.DefaultIfEmpty()

					where
					nt.CreatedDate >= filter.From
					&& nt.CreatedDate <= filter.To
					//&& (userData.IsSysAdmin || (userRoles.Contains(nt.IntendedRole) || nt.IntendedUser == userData.Principal.Identity.Name))
					&& (filter.Type == NotificationType.All || nt.Type == filter.Type)
					&& (userData.IsSysAdmin || nt.LocationID == userData.LocationID)
					//&& (!filter.CompanyID.HasValue || filter.CompanyID == 0 || cmp.ID == filter.CompanyID)
					select nt;

            var owner = NotificationRepository.BelongToBase(userData);
            q = q.Where(owner);

            return q.Count();
		}

		public void Dismiss(int id)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				Dismiss(ctx, id);
			}
		}


		public void Dismiss(PrintDB ctx, int id)
		{
			var notification = GetByID(ctx, id);
			if (notification != null)
				Delete(ctx, id);
		}


		public void DismissKey(string key)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				DismissKey(ctx, key);
			}
		}


		public void DismissKey(PrintDB ctx, string key)
		{
			var notification = ctx.Notifications.Where(p => p.NKey == key).SingleOrDefault();
			if (notification != null)
				Delete(ctx, notification.ID);
		}


		public void AddNotification(
			int companyid, NotificationType type, string intendedRoles, string intendedUser,
			string nkey, string source, string title, string message, object data,
			bool autoDismiss, int? locationID, int? projectID, string actionController = null)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				AddNotification(ctx, companyid, type, intendedRoles, intendedUser, nkey, source, title, message, data, autoDismiss, locationID, projectID, actionController);
			}
		}

		public void AddNotification(
			PrintDB ctx, int companyid, NotificationType type, string intendedRoles, string intendedUser,
			string nkey, string source, string title, string message, object data,
			bool autoDismiss, int? locationID, int? projectID, string actionController = null)
		{
			try
			{
				var notification = Create();
				notification.CompanyID = companyid;
				notification.Type = type;
				notification.IntendedRole = intendedRoles;
				notification.IntendedUser = intendedUser;
				notification.NKey = nkey;
				notification.Source = source;
				notification.Title = title;
				notification.Message = message;
				notification.Data = JsonConvert.SerializeObject(data);
				notification.AutoDismiss = autoDismiss;
				notification.Action = actionController;
				notification.LocationID = locationID;
				notification.ProjectID = projectID;
				Insert(ctx, notification);
			}
			catch { }
		}


		public void SendIDTErrorEmail(
			string key,
			EmailType emailType,
			ErrorNotificationType errorType,
			int projectid,
			int? productionLocation,
			string title,
			string message,
			int? orderid)
		{
			var recipients = GetIDTStakeholders(projectid, productionLocation);
			var mailService = factory.GetInstance<IOrderEmailService>();
			foreach (var usr in recipients)
			{
				var token = mailService.GetTokenFromUser(usr, emailType);
				if (token != null)
					mailService.AddErrorIfNotExist(
						token: token,
						errorType: errorType,
						title: title,
						message: message,
						key: key,
						projectId: projectid,
						locationId: productionLocation,
						orderId: orderid
						);
			}
		}

        public void SendErrorEmail(
            string key
            , string title
            , string message
            , IEnumerable<string> stakeHolders
            , EmailType emailType
            , ErrorNotificationType errorType
            , int projectid
            , int companyID
            , int? productionLocation
            , int? orderid)
        {
            var mailService = factory.GetInstance<IOrderEmailService>();
            foreach (var usr in stakeHolders)
            {
                var token = mailService.GetTokenFromUser(usr, emailType);
                if (token != null)
                    mailService.AddErrorIfNotExist(
                        token: token,
                        errorType: errorType,
                        title: title,
                        message: message,
                        key: key,
                        projectId: projectid,
                        locationId: productionLocation,
                        orderId: orderid
                        );
            }
        }

        /// <summary>
        /// REturn users IDs GUID
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public IEnumerable<string> GetIDTStakeholders(int projectId, int? locationId)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetIDTStakeholders(ctx, projectId, locationId).ToList();
            }
        }

        public IEnumerable<string> GetIDTStakeholders(PrintDB ctx, int projectId, int? locationId)
        {
            // get customer service person for this project or company
            var projectRepo = factory.GetInstance<IProjectRepository>();
            var userRepo = factory.GetInstance<IUserRepository>();

            var recipients = projectRepo.GetCustomerEmails(ctx, projectId);
            var prodManagers = userRepo.GetProdManagers(locationId)
                .Select(s => s.Id);

            recipients.AddRange(prodManagers);

            return recipients;
        }

        private static Expression<Func<Notification, bool>> BelongToBase(IUserData userData)
        {
            var predicate = PredicateBuilder.New<Notification>();

            predicate.Or(p => userData.IsSysAdmin);
            predicate.Or(p => p.IntendedUser.Equals(userData.Principal.Identity.Name));

            foreach (var r in userData.UserRoles)
                predicate.Or(p => p.IntendedRole.Contains(r));

            return predicate;
        }

        private static Expression<Func<INotification, bool>> BelongTo(IUserData userData)
        {
            var predicate = PredicateBuilder.New<INotification>();

            predicate.Or(p => userData.IsSysAdmin);
            predicate.Or(p => p.IntendedUser.Equals(userData.Principal.Identity.Name));

            foreach (var r in userData.UserRoles)
                predicate.Or(p => p.IntendedRole.Contains(r));

            return predicate;
        }

        private static Expression<Func<NotificationReportDTO, bool>> BelongToInReport(IUserData userData)
        {
            var predicate = PredicateBuilder.New<NotificationReportDTO>();

            predicate.Or(p => userData.IsSysAdmin);
            predicate.Or(p => p.IntendedUser.Equals(userData.Principal.Identity.Name));

            foreach (var r in userData.UserRoles)
                predicate.Or(p => p.IntendedRole.Contains(r));

            return predicate;
        }

    }
}
