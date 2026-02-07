using Service.Contracts;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	// ==================================================================================================
	// When triggered it creates the print package for the specified order.
	// ==================================================================================================
	public class CreatePrintPackage: EQEventHandler<OrderDocumentsCompletedEvent>
	{
		private IPrintPackageService pps;
		private IOrderRepository orderRepo;
		private IProjectRepository projectRepo;
		private ILogService log;
		

		public CreatePrintPackage(
			IOrderRepository orderRepo,
			IProjectRepository projectRepo,
			IPrintPackageService pps,
			ILogService log
		)
		{
			this.orderRepo = orderRepo;
			this.projectRepo = projectRepo;
			this.pps = pps;
			this.log = log;
		}

		public override EQEventHandlerResult HandleEvent(OrderDocumentsCompletedEvent e)
		{
            // if order has order workflow dont't process the event
            var order = orderRepo.GetByID(e.OrderID);
            if (order.HasOrderWorkflow)
                return EQEventHandlerResult.OK;
            
			var project = projectRepo.GetByID(order.ProjectID);
			if (order.ProductionType != ProductionType.CustomerLocation && project.DisablePrintLocal == false)
			{
				pps.CreatePrintPackage(e.OrderID);
			}
			else
			{
				log.LogMessage($"PrintPackage for order [{e.OrderID}] will not be created because the order ProductionType is set to CustomerLocation or Print Local is disabled in the project configuration (ProjectID: {project.ID}).");
			}

			return new EQEventHandlerResult() { Success = true };
		}
	}
}
