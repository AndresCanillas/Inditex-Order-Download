using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintCentral.Utilities;
using Service.Contracts;
using Services.Core;
using System;

namespace PrintCentral.Controllers
{
    [Authorize]
    public class UtilityController : Controller
    {
        private ILogService log;

        public UtilityController(ILogService log)
        {
            this.log = log;
        }

        [HttpGet, Route("/utility/getstatusservice/{service}")]
        public string GetStatusService(string service)
        {
            try
            {
                return ServiceStatus.GetWindowsServiceStatus(service).ToString();
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpPost, Route("/utility/startservice/{service}")]
        public OperationResult StartService(string service)
        {
            try
            {               
                return new OperationResult(true, ServiceStatus.StartWindowsService(service).ToString(), null);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, ex.Message, null);
            }
        }
    }
}