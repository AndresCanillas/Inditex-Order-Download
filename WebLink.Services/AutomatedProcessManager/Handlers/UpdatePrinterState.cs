using System;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	// ================================================================================================
	// Updates the LastSeen field of a printer when said printer becomes online.
	// ================================================================================================
	public class UpdatePrinterState : EQEventHandler<PrinterConnectedEvent>
	{
		private IFactory factory;
		private ILogService log;

		public UpdatePrinterState(IFactory factory, ILogService log)
		{
			this.factory = factory;
			this.log = log;
		}

		public override EQEventHandlerResult HandleEvent(PrinterConnectedEvent e)
		{
			try
			{
				IPrinterRepository repo = factory.GetInstance<IPrinterRepository>();
				repo.UpdateLastSeen(e.DeviceID, e.ProductName, e.Firmware);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
			return new EQEventHandlerResult() { Success = true };
		}
	}
}
