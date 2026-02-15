using Microsoft.AspNetCore.Mvc;
using OrderDonwLoadService.Synchronization;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OrderDownloadWebApi.Controllers
{
    public class OrdersController : Controller
    {
        private static readonly Regex QueueFoundMessageRegex = new Regex(@"^Order number \((.+)\) found successfully in (.+) queue\.$", RegexOptions.Compiled);
        private static readonly Regex QueueNotFoundMessageRegex = new Regex(@"^Order number \((.+)\) not found in any queue\.$", RegexOptions.Compiled);

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
        public async Task<OperationResult> GetOrder([FromBody] GetOderDto OrderDto)
        {
            try
            {
                var message = await orderServices.GetOrder(OrderDto.OrderNumber, OrderDto.CampaignCode, OrderDto.VendorId);
                var localizedMessage = LocalizeOrderMessage(message);

                if (message?.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new OperationResult(false, localizedMessage);
                }

                return new OperationResult(true, localizedMessage, null);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        private string LocalizeOrderMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            var lines = message.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var localizedLines = new List<string>(lines.Length);

            foreach (var line in lines)
            {
                localizedLines.Add(LocalizeOrderMessageLine(line));
            }

            return string.Join(Environment.NewLine, localizedLines);
        }

        private string LocalizeOrderMessageLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return line;
            }

            var queueFoundMatch = QueueFoundMessageRegex.Match(line);
            if (queueFoundMatch.Success)
            {
                return g["Order number ({0}) found successfully in {1} queue.", queueFoundMatch.Groups[1].Value, queueFoundMatch.Groups[2].Value];
            }

            var queueNotFoundMatch = QueueNotFoundMessageRegex.Match(line);
            if (queueNotFoundMatch.Success)
            {
                return g["Order number ({0}) not found in any queue.", queueNotFoundMatch.Groups[1].Value];
            }

            return g[line];
        }
    }

    public class GetOderDto
    {
        public string OrderNumber { get; set; }
        public string CampaignCode { get; set; }
        public string VendorId { get; set; }

    }
}
