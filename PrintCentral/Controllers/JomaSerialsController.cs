using Microsoft.AspNetCore.Mvc;
using Service.Contracts.Infrastructure.Encoding.SerialSequences;
using Services.Core;
using System;
using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers
{
    public class JomaSerialsController : Controller
    {
        private readonly ILogService log;
        private readonly INotificationRepository notification;
        private IJomaSerialSequence sequence;

        public JomaSerialsController(IJomaSerialSequence sequence)
        {
            this.sequence = sequence;
        }

        [HttpPost, Route("/serialnumber/getjomaserials")]
        public List<long> GetSerials([FromBody] int count)
        {
            try
            {
                return sequence.AcquireMultiple(count);
            }
            catch(Exception ex)
            {
                log.LogMessage(ex.Message);
                throw;
            }
        }
    }
}
