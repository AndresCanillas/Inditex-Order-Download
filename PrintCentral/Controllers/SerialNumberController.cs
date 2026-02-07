using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers
{
    public class SerialNumberController : Controller
    {
        private readonly ISerialRepository repo;
        private readonly ILogService log;
        private readonly INotificationRepository notification;

        public SerialNumberController(ISerialRepository repo, ILogService log, INotificationRepository notification)
        {
            this.repo = repo;
            this.log = log;
            this.notification = notification;
        }

        [HttpPost, Route("/serialnumber/getserials")]
        public long GetSerials([FromBody]GetSerialsRQ request)
        {
            try
            {
                return repo.AcquireSequential(request.Sequence, request.Count);
            }
            catch(Exception ex)
            {
                notification.AddNotification(1, NotificationType.Error, "ProdManager", null, request.Sequence.ID, "SerialNumberController", "AcquireSequential", ex.Message, request, false, null, null);
                log.LogMessage(ex.Message);
                throw;
            }
        }
    }
}