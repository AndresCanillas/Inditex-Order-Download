using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Services
{
    public class MonoprixSendEANReportHandler : EQEventHandler<MonoprixDailyEANReportEvent>
    {
        private readonly IMonoprixRFIDReportGeneratorService reportService;

        public MonoprixSendEANReportHandler(IMonoprixRFIDReportGeneratorService reportService)
        {
            this.reportService = reportService;
        }

        public override EQEventHandlerResult HandleEvent(MonoprixDailyEANReportEvent e)
        {
            try
            {
                reportService.SendReport(e.CompanyID, e.StartDate, e.EndDate);
            }catch(Exception ex)
            {
                throw new MonoprixSendEANReportHandlerException($"Cannot send Monoprix report from: [{e.StartDate}] to: [{e.EndDate}], see the inner exception", ex);
            }


            return EQEventHandlerResult.OK;
        }

        
    }

    [Serializable]
    public class MonoprixSendEANReportHandlerException : Exception
    {
        public MonoprixSendEANReportHandlerException()
        {
        }

        public MonoprixSendEANReportHandlerException(string message) : base(message)
        {
        }

        public MonoprixSendEANReportHandlerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MonoprixSendEANReportHandlerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }


}
