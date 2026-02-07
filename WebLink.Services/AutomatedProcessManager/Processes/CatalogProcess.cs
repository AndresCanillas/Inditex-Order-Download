using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
    public class CatalogProcess : IAutomatedProcess
    {
        private Dictionary<string, string> tokens;

        private IFactory factory;
        private IEventQueue events;
        //private ILocalizationService g;

        public CatalogProcess(
            IFactory factory,
            IEventQueue eventService
            )
        {
            tokens = new Dictionary<string, string>();
            this.factory = factory;
            this.events = eventService;
        }

        public TimeSpan GetIdleTime()
        {
            return TimeSpan.MaxValue;  // returning MaxValue means that this process does not execute at regular intervals
        }

        public void OnExecute()
        {
            throw new NotImplementedException();
        }

        public void OnLoad()
        {
            tokens["CatalogStructureUpdateEvent"] = events.Subscribe<CatalogStructureUpdatedEvent>(CatalogStructureUpdatedHandler);
            tokens["CatalogNameUpdateEvent"] = events.Subscribe<CatalogNameUpdatedEvent>(CatalogNameUpdatedHandler);
            tokens["CatalogDataUpdateEvent"] = events.Subscribe<CatalogDataUpdatedEvent>(CatalogDataUpdatedHandler);
        }

        public void OnUnload()
        {
            events.Unsubscribe<CatalogStructureUpdatedEvent>(tokens["CatalogStructureUpdateEvent"]);
            events.Unsubscribe<CatalogNameUpdatedEvent>(tokens["CatalogNameUpdateEvent"]);
            events.Unsubscribe<CatalogDataUpdatedEvent>(tokens["CatalogDataUpdateEvent"]);
        }


        // look order affected by catalog id received and turn on HoldFlag
        // send notification to the users
        private void CatalogStructureUpdatedHandler(CatalogStructureUpdatedEvent e)
        {
            var notificationsRepo = factory.GetInstance<INotificationRepository>();
            
            var orderRepo = factory.GetInstance<IOrderRepository>();

            //var catalogRepo = sp.GetRequiredService<ICatalogRepository>();

            //var catalog = catalogRepo.GetByID(e.CatalogID);

            //var orders = orderRepo.ToNotifyCatalogsUpdated(e.ProyectID);

            var total = orderRepo.TotalOrdersAffectedByCatalogupdate(e.ProjectID);

            e.TotalOrders = total;

            // Change to check if configured as QA
            //#if DEBUG
            //			// always add notification
            //			total = 1;
            //#endif
            if (total < 1)
            {
                return;
            }

            notificationsRepo.AddNotification(
                companyid:          1,                              // ???: this notification is only for IDT
                type:               NotificationType.OrderTracking, // ???: maybe required create new type
                intendedRoles:      string.Empty,                   // ???: check roles required
                intendedUser:       string.Empty,
                nkey:               "CatalogStructureUpdatedEvent",  // ???: how to defined NKey
                source:             NotificationSources.SystemMessages,
                title:              "Catalogs Structure was Updated", 
                message:            "Require Review orders Affected",
                data:               e,                              // ???: use even like data 
                autoDismiss:        false,                          // no autodismss
                locationID:         null,
                projectID:          e.ProjectID,
                actionController:   "OrderAffectedNotificationView"// custom page
                );



        }


        private void CatalogNameUpdatedHandler(CatalogNameUpdatedEvent e)
        {
            var notificationsRepo = factory.GetInstance<INotificationRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            var total = orderRepo.TotalOrdersAffectedByCatalogupdate(e.ProjectID);

            e.TotalOrders = total;

			// Change to check if configured as QA
			//#if DEBUG
			//            // always add notification
			//            total = 1;
			//#endif
			if (total < 1)
            {
                return;
            }

            notificationsRepo.AddNotification(
                companyid:          1,                              // ???: this notification is only for IDT
                type:               NotificationType.OrderTracking, // ???: maybe required create new type
                intendedRoles:      string.Empty,                   // ???: check roles required
                intendedUser:       string.Empty,
                nkey:               "CatalogNameUpdatedEvent",  // ???: how to defined NKey
                source:             NotificationSources.SystemMessages,                          // ???: source types where are defined
                title:              "Catalogs Structure was Updated",
                message:            "Require Review orders Affected",
                data:               e,                              // ???: use even like data 
                autoDismiss:        false,                          // no autodismss
                locationID:         null,
                projectID:          e.ProjectID,
                actionController:   "OrderAffectedNotificationView"// custom page
                );
        }

        private void CatalogDataUpdatedHandler(CatalogDataUpdatedEvent e)
        {
            var notificationsRepo = factory.GetInstance<INotificationRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            var total = orderRepo.TotalOrdersAffectedByCatalogupdate(e.ProjectID);

            e.TotalOrders = total;

            if (total < 1)
            {
                return;
            }

            notificationsRepo.AddNotification(
                companyid:          1,                                          // ???: this notification is only for IDT
                type:               NotificationType.OrderTracking,             // ???: maybe required create new type
                intendedRoles:      string.Empty,                               // ???: check roles required
                intendedUser:       string.Empty,
                nkey:               "CatalogDataUpdatedEvent",                  // ???: how to defined NKey
                source:             NotificationSources.SystemMessages,         // ???: source types where are defined
                title:              "Catalogs Structure was Updated",
                message:            "Require Review orders and labels Affected",
                data:               e,                                          // ???: use even like data 
                autoDismiss:        false,                                      // no autodismss
                locationID:         null,
                projectID:          e.ProjectID,
                actionController:   "OrderAffectedNotificationView"             // custom page
                );
        }
    }
}
