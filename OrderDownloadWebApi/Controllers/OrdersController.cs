using Microsoft.AspNetCore.Mvc;
using OrderDonwLoadService.Synchronization;
using Service.Contracts;
using System;
using System.Threading.Tasks;

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

        [HttpPost, Route("/order/get/")]
        public async Task<OperationResult> GetOrder([FromBody] GetOderDto  OrderDto)
        {
            try
            {
                var message = await orderServices.GetOrder(OrderDto.OrderNumber,OrderDto.CampaignCode,OrderDto.VendorId);
                if (message?.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new OperationResult(false, g[message]);
                }

                return new OperationResult(true, g[message], null);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

    }
    public class GetOderDto
    {
        public string OrderNumber { get; set; }
        public string CampaignCode { get; set; }
        public string VendorId { get; set; }

    }

}
