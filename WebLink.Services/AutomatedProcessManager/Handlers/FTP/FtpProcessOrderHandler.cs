using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;

namespace WebLink.Services.Automated
{
    [Obsolete("Replace by IntakeWorkflow")]
    public class FtpProcessOrderHandler : EQEventHandler<FtpFileReceivedEvent>
    {
        private IProcessOrderFileService orderFileService;

        public FtpProcessOrderHandler(IProcessOrderFileService orderFileService)
        {
            this.orderFileService = orderFileService;
        }

        public override EQEventHandlerResult HandleEvent(FtpFileReceivedEvent e)
        {

            if (orderFileService.FileIsPending(e.FtpFileReceivedID))
            {
                var result = orderFileService.ProcessFile(e.FtpFileReceivedID).Result;

                if (!result.Success || result.Errors.Count > 0)
                {
                    e.RetryCount++;
                    var delay = 10 + e.RetryCount * 2;
                    return new EQEventHandlerResult() { Success = false, Delay = TimeSpan.FromMinutes(delay) };
                }
            }
            return EQEventHandlerResult.OK;
        }
    }
}
