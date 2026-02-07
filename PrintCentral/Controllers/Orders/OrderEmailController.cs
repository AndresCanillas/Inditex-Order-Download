using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Service.Contracts;
using Services.Core;
using System;
using System.IO;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers
{
    public class OrderEmailController : Controller
    {
        private IOrderEmailService emailService;
        private ILocalizationService g;
        private ILogService log;


        public OrderEmailController(
            IOrderEmailService emailService,
            ILocalizationService g,
            ILogService log)
        {
            this.emailService = emailService;
            this.g = g;
            this.log = log;
        }


        [HttpPost, Route("/orderemail/markasseen")]
        public OperationResult MarkAsSeen([FromBody] MarkAsSeenOrderRequest rq)
        {
            try
            {
                var token = emailService.GetTokenFromCode(rq.TokenCode);
                if (token != null)
                    emailService.MarkAsSeen(token, rq.OrderIDs);
                return new OperationResult(true, g["Done!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }


        [HttpPost, Route("/orderemail/updatesettings")]
        public OperationResult UpdateSettings([FromBody] UpdateSettingsRequest rq)
        {
            try
            {
                var token = emailService.GetTokenFromCode(rq.TokenCode);
                if (token != null)
                    emailService.UpdateEmailServiceSettings(token, rq.Settings);
                return new OperationResult(true, g["Done!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }


        [HttpGet, Route("/orderemail/preview/{tokenCode}/{orderid}")]
        public IActionResult GetFont(string tokenCode, int orderid)
        {
            try
            {
                var token = emailService.GetTokenFromCode(tokenCode);
                if (token != null)
                {
                    IAttachmentData attachment = emailService.GetOrderPreview(token, orderid);
                    if(attachment != null)
					{
                        Response.Headers.Add("X-Content-Type-Options", "nosniff");
                        Response.Headers[HeaderNames.CacheControl] = "no-cache";
                        return File(attachment.GetContentAsStream(), MimeTypes.GetMimeType(Path.GetExtension(attachment.FileName)), attachment.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogException(ex);
            }
            return NotFound();
        }
    }


    public class MarkAsSeenOrderRequest
	{
        public string TokenCode;
        public int[] OrderIDs;
	}

    public class UpdateSettingsRequest
	{
        public string TokenCode;
        public EmailServiceSettings Settings;
    }
}
