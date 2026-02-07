using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.OrderEvents;
using WebLink.Contracts.Services;

namespace WebLink.Services
{
    public class ArmandThieryRFIDReportHandler : EQEventHandler<ArmandThieryDailyEANsReportEvent>
    {
        private readonly IArmandThieryRFIDReportGeneratorService reportService;
        private readonly IAppLog log;

        public ArmandThieryRFIDReportHandler(IArmandThieryRFIDReportGeneratorService reportService, IAppLog log)
        {
            this.reportService = reportService;
            this.log = log;
        }

        public override EQEventHandlerResult HandleEvent(ArmandThieryDailyEANsReportEvent e)
        {
            reportService.SendReport(e.CompanyID);

            return EQEventHandlerResult.OK;
        }
    }

    [Serializable]
    internal class ArmandThieryRFIDReportGeneratorServiceException : Exception
    {
        public ArmandThieryRFIDReportGeneratorServiceException()
        {
        }

        public ArmandThieryRFIDReportGeneratorServiceException(string message) : base(message)
        {
        }

        public ArmandThieryRFIDReportGeneratorServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ArmandThieryRFIDReportGeneratorServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
