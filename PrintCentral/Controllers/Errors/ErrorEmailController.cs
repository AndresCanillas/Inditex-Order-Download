using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers
{
    public class ErrorEmailController : Controller
    {
        private IOrderEmailService emailService;
        private ILocalizationService g;
        private ILogService log;


        public ErrorEmailController(
            IOrderEmailService emailService,
            ILocalizationService g,
            ILogService log)
        {
            this.emailService = emailService;
            this.g = g;
            this.log = log;
        }


        [HttpPost, Route("/erroremail/markasseen")]
        public OperationResult MarkAsSeen([FromBody] MarkAsSeenErrorRequest rq)
        {
            try
            {
                var token = emailService.GetTokenFromCode(rq.TokenCode);
                if (token != null)
                    emailService.MarkErrorAsSeen(token, rq.ErrorIDs);
                return new OperationResult(true, g["Done!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

    }

    public class MarkAsSeenErrorRequest
	{
        public string TokenCode;
        public int[] ErrorIDs;
	}
}
