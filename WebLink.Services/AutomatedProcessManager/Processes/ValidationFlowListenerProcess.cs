using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
    public class ValidationFlowListenerProcess : IAutomatedProcess
    {
        private IFactory factory;
        private IEventQueue events;
        private ILocalizationService g;
        private IOrderDocumentService docSrv;
        private INotificationRepository notificationRepo;

        public ValidationFlowListenerProcess(
            IFactory factory,
            IEventQueue eventService,
            ILocalizationService gobalService,
            IOrderDocumentService docSrv,
            INotificationRepository notificationRepository
            )
        {
            this.factory = factory;
            this.events = eventService;
            this.g = gobalService;
            this.docSrv = docSrv;
            this.notificationRepo = notificationRepository;
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
            events.Subscribe<QuantitiesStepStartedEvent>(QuantitiesStepStartedHandler);
            events.Subscribe<QuantitiesStepCompletedEvent>(QuantitiesStepCompletedHandler);

            events.Subscribe<ArticlesExtraStepStartedEvent>(ArticlesExtrasStartedHandler);
            events.Subscribe<ArticlesExtraStepCompletedEvent>(ArticlesExtrasCompletedHandler);

            events.Subscribe<AddressStepStartedEvent>(AddressStepStartedHandler);
            events.Subscribe<AddressStepCompletedEvent>(AddressStepCompletedHandler);

            events.Subscribe<ReviewStepStartedEvent>(ReviewStepStartedHandler);
            events.Subscribe<ReviewStepCompletedEvent>(ReviewStepCompletedHandler);

            events.Subscribe<OrderValidatedEvent>(OrderValidatedHandler);

            //events.Subscribe<ReviewValidatedEvent>(ReviewValidatedHandler); 

        }

        public void OnUnload()
        {
        }


        private void QuantitiesStepStartedHandler(QuantitiesStepStartedEvent e)
        {

        }

        private void QuantitiesStepCompletedHandler(QuantitiesStepCompletedEvent e)
        {

        }

        private void ArticlesExtrasStartedHandler(ArticlesExtraStepStartedEvent e)
        {

        }

        private void ArticlesExtrasCompletedHandler(ArticlesExtraStepCompletedEvent e)
        {

        }

        private void AddressStepStartedHandler(AddressStepStartedEvent e)
        {
            if(!docSrv.PreviewDocumentExists(e.OrderID, out _))
                _ = docSrv.CreatePreviewDocument(e.OrderID);
        }

        private void AddressStepCompletedHandler(AddressStepCompletedEvent e)
        {

        }

        private void ReviewStepStartedHandler(ReviewStepStartedEvent e)
        {

        }

        private void ReviewStepCompletedHandler(ReviewStepCompletedEvent e)
        {

        }

        private void ReviewValidatedHandler (ReviewStepCompletedEvent e)
        {
            /* NO USED*/
        }

        private void OrderValidatedHandler(OrderValidatedEvent e)
        {

        }

    }
}
