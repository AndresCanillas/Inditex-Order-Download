using Service.Contracts;
using Service.Contracts.Database;
using System;
using Microsoft.Extensions.DependencyInjection;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Processing
{
    // ===============================================================================
    // Get Encoded jobs barcodes to count on locasl db 
    //
    // Response contains JobId and Encoded amount to update Job on Central DB

    // ===============================================================================
    //public class UpdatePendingEncodedJobsHandler : EQEventHandler<JobEncoded>
    //{
    //    private ILogService log;
    //    private readonly IServiceProvider sp;

    //    public UpdatePendingEncodedJobsHandler(ILogService log, IServiceProvider sp)
    //    {
    //        this.log = log;
    //        this.sp = sp;
    //    }

    //    public override void HandleEvent(JobEncoded e)
    //    {
    //        var connManager = sp.GetRequiredService<IDBConnectionManager>();
    //        using (var conn = connManager.OpenWebLinkDB())
    //        {
    //            conn.ExecuteNonQuery(@"
				//	update PrinterJobs
				//	set Encoded = @encoded, Status = 5
				//	where ID = @id", e.Encoded, e.JobID);
    //        }
    //    }
    //}
}
