using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Services.Zebra
{
	public class PrintJob : IPrintJob
	{
		private static MemorySequence sequence = new MemorySequence();

		private string id;
		private volatile JobState state = JobState.Pending;
		private int quantity;
		private List<PrintTaskStatus> statuses;
		private volatile bool cancelled = false;
		private DateTime createdDate;
		private DateTime completedDate;

		public PrintJob()
		{
			id = $"Job-{sequence.NextID()}";
			createdDate = DateTime.Now;
			statuses = new List<PrintTaskStatus>();
		}

		public void SetTasks(PrintTask task)
		{
			SetTasks(new PrintTask[] { task });
		}

		public void SetTasks(IEnumerable<PrintTask> tasks)
		{
			if (state != JobState.Pending)
				throw new Exception("SetTasks can only be called before the job is started.");
			quantity = 0;
			statuses.Clear();
			foreach(var task in tasks)
			{
				if (task.Quantity <= 0)
					continue;
				var t = new PrintTaskStatus()
				{
					LabelName = task.LabelName,
					SKU = task.SKU,
					Quantity = task.Quantity
				};
				statuses.Add(t);
				quantity += task.Quantity;
			}
			Progress = 0;
		}

		public string ID { get => id; }

		public string DeviceID { get; set; }

		public string Name { get; set; }

		public bool PrintHeaders { get; set; }

		public JobState State
		{
			get => state;
			set
			{
				state = value;
				if (Completed)
					completedDate = DateTime.Now;
			}
		}

		public string Error { get; set; }

		public int Quantity { get => quantity; }

		public int Progress { get; set; }

		public List<PrintTaskStatus> Statuses { get => statuses; }

		public void Cancel()
		{
			cancelled = true;
			State = JobState.Cancelled;
		}

		public bool Cancelled { get => cancelled; }

		public bool Completed { get => (int)State >= (int)JobState.Completed; }

		public DateTime CreatedDate { get => createdDate; }

		public DateTime CompletedDate { get => completedDate; }

	}
}
