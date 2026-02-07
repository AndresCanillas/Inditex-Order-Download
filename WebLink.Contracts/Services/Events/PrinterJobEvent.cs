using Service.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public class PrinterJobEvent : EQEventInfo
	{
		public PrinterJobEventType Type { get; set; }
		public object Data { get; set; }

		public PrinterJobEvent(int companyid, PrinterJobEventType type, object data)
		{
			CompanyID = companyid;
			Type = type;
			switch (Type)
			{
				case PrinterJobEventType.JobStatusUpdate:
					Data = new JobStatusUpdateDTO(data as IPrinterJob);
					break;
				default:
					Data = data;
					break;
			}
		}
	}

	public enum PrinterJobEventType
	{
		JobCreated = 1,
		JobStatusUpdate = 2,
		JobDetailUpdate = 3,
		JobError = 4,
		LocationChanged = 5,
		PrinterChanged = 6,
		PrinterStatus = 7,
		AllPrinterStatus = 8,
		ExtrasAdded = 9
	}

	public class JobStatusUpdateDTO
	{
		public int ID { get; set; }
		public int Status { get; set; }
		public int Printed { get; set; }
		public JobStatusUpdateDTO() { }
		public JobStatusUpdateDTO(IPrinterJob job)
		{
			ID = job.ID;
			Status = (int)job.Status;
			Printed = job.Printed;
		}
	}
}
