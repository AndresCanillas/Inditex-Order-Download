using Service.Contracts;
using System;
using System.Runtime.Serialization;
using WebLink.Contracts.OrderEvents;
using WebLink.Contracts.Services;

namespace WebLink.Services
{
    public class BrownieSendReportHandler : EQEventHandler<BrownieDailyReportEvent>
    {
        private readonly IBrownieReportGeneratorService _reportService;

        public BrownieSendReportHandler(IBrownieReportGeneratorService reportService)
        {
            _reportService = reportService;
        }

        public override EQEventHandlerResult HandleEvent(BrownieDailyReportEvent e)
        {
            _reportService.SendReport(e.CompanyID, e.StartDate, e.EndDate);
            return EQEventHandlerResult.OK;
        }
    }
    [Serializable]
    internal class BrownieReportGeneratorServiceException : Exception
    {
        public BrownieReportGeneratorServiceException()
        {
        }

        public BrownieReportGeneratorServiceException(string message) : base(message)
        {
        }

        public BrownieReportGeneratorServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BrownieReportGeneratorServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
