using Service.Contracts;
using Services.Core;
using System;
using System.Runtime.Serialization;
using WebLink.Contracts;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Services
{
    public class BandFSendRFIDReportHandler : EQEventHandler<BandFDailyEANReportEvent>
    {
        private readonly IBandFRFIDReportGeneratorService reportService;
        private readonly ILogSection log;

        public BandFSendRFIDReportHandler(IBandFRFIDReportGeneratorService reportService, ILogService log)
        {
            this.reportService = reportService;
            this.log = log.GetSection("ReverseFlow");
        }

        public override EQEventHandlerResult HandleEvent(BandFDailyEANReportEvent e)
        {

            //try
            //{
                reportService.SendReport(e.CompanyID, e.StartDate, e.EndDate);
            //}
            //catch (Exception ex)
            //{
            //    throw new BandFRFIDReportGeneratorServiceException($"B&F - Cannot send RFID report see the inner exception", ex);
            //}
            

            return EQEventHandlerResult.OK;
        }
        
    }

    [Serializable]
    internal class BandFRFIDReportGeneratorServiceException : Exception
    {
        public BandFRFIDReportGeneratorServiceException()
        {
        }

        public BandFRFIDReportGeneratorServiceException(string message) : base(message)
        {
        }

        public BandFRFIDReportGeneratorServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BandFRFIDReportGeneratorServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
