using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public class PrinterJobError
	{
		public string Message { get; set; }
		public IPrinterJob Job { get; set; }
		public PrinterJobError() { }
		public PrinterJobError(string message, IPrinterJob job)
		{
			Message = message;
			Job = job;
		}
	}
}
