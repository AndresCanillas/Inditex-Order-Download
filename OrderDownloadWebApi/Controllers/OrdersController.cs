using Microsoft.AspNetCore.Mvc;
using OrderDonwLoadService.Synchronization;
using Service.Contracts;
using System;

namespace OrderDownloadWebApi.Controllers
{
    public class OrdersController : Controller
    {
        private ILocalizationService g;
        private IAppLog log;
        private IOrderServices orderServices;

        public OrdersController(
            ILocalizationService g,
            IAppLog log,
            IOrderServices orderServices)
        {
            this.g = g;
            this.log = log;
            this.orderServices = orderServices;
        }


        [HttpGet, Route("/order/getbackonthequeue/{number}")]
        public OperationResult GetBackOnTheQueue(string number)
        {
            try
            {
                var message = orderServices.GetBackOnTheQueue(number).Result;
                if(message.Contains("Error"))
                {
                    return new OperationResult(false, g[message]);
                }

                return new OperationResult(true, g[message], null);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

    }
}