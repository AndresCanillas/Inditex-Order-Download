using Service.Contracts;
using Service.Contracts.PrintLocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    public class ProcessUnitProgressChange : EQEventHandler<PLUnitProgressChangeEvent>
    {
        private readonly PrintDB ctx;
        private readonly IPrinterJobRepository repo;
        private readonly IOrderLogService log;

        public ProcessUnitProgressChange(PrintDB ctx, IPrinterJobRepository repo, IOrderLogService log)
        {
            this.ctx = ctx;
            this.repo = repo;
            this.log = log;
        }

        public override EQEventHandlerResult HandleEvent(PLUnitProgressChangeEvent e)
        {
            var detail = ctx.PrinterJobDetails
                            .FirstOrDefault(pjd => pjd.ID == e.PrintJobDetailID);

            if(detail != null)
            {
                var job = ctx.PrinterJobs
                             .FirstOrDefault(pj => pj.ID == detail.PrinterJobID);

                if(job != null)
                {
                    var article = ctx.Articles
                                     .FirstOrDefault(a => a.ID == job.ArticleID);

                    if(article != null)
                    {
                        repo.UpdateDetailProgress(e);
                    }
                }
            }

            return new EQEventHandlerResult();
        }
    }
}

