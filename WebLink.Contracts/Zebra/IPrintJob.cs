using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace WebLink.Contracts
{
	public interface IPrintJob
	{
		string ID { get; }
		string DeviceID { get; set; }
		string Name { get; set; }
		JobState State { get; set; }
		string Error { get; set; }
		int Quantity { get; }
		int Progress { get; set; }
		bool PrintHeaders { get; set; }
		List<PrintTaskStatus> Statuses { get; }
		void Cancel();
		bool Cancelled { get; }
		bool Completed { get; }
		DateTime CreatedDate { get; }
		DateTime CompletedDate { get; }
		void SetTasks(PrintTask task);
		void SetTasks(IEnumerable<PrintTask> tasks);
	}

	public enum JobState
	{
		Pending,                    // Job is queued but still not executing
		Starting,                   // Job is being started
		SendingWork,                // Job is actively printing labels
		PrinterNotReady,            // Job is waiting for the printer to be ready again
		PrinterOffline,				// Job is waiting because the printer is not connected
		PrinterPaused,				// Job is waiting because the printer is paused
		Paused,						// Job is waiting beacuse of a user request
		Completed,                  // Job has been completed
		Cancelled,                  // Job has been cancelled by the user
		InternalError               // Job has been aborted by an internal error (it will not be resumed / retried)
	}

	public class PrintTask
	{
		public PrintTask(string label, string sku, int quantity)
		{
			LabelName = label;
			SKU = sku;
			Quantity = quantity;
		}

		public string LabelName { get; set; }
		public string SKU { get; set; }
		public int Quantity { get; set; }

	}

	public class PrintTaskStatus
	{
		public bool Printing { get; set; }
		public string LabelName { get; set; }
		public string SKU { get; set; }
		public int Quantity { get; set; }
		public int Progress { get; set; }
		public int PrintCount { get; set; }
		public int ErrorCount { get; set; }
	}

}
