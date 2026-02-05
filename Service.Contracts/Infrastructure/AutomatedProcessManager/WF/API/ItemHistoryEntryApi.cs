using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class ItemHistoryEntryApi : IItemHistoryEntry
	{
		private ItemLog data;
		private TaskHistoryLogData historyData;

		public ItemHistoryEntryApi(ItemLog data)
		{
			this.data = data;
			this.historyData = JsonConvert.DeserializeObject<TaskHistoryLogData>(data.AttachedData);
		}

		public long EntryID { get => data.EntryID; }
		public int WorkflowID { get => data.WorkflowID; }
		public long ItemID { get => data.ItemID; }
		public ItemLogEntryType EntryType { get => ItemLogEntryType.TaskHistory; }
		public int TaskID { get => historyData.TaskID; }
		public string TaskName { get => historyData.TaskName; }
		public string Message { get => historyData.Message; }
		public string Status { get => historyData.Status; }
		public string StatusReason { get => historyData.StatusReason; }
		public string RouteCode { get => historyData.RouteCode; }
		public int DelayTime { get => historyData.DelayTime; }
		public bool Success { get => historyData.Success; }
		public string ExceptionType { get => historyData.ExceptionType; }
		public string ExceptionMessage { get => historyData.ExceptionMessage; }
		public string ExceptionStackTrace { get => historyData.ExceptionStackTrace; }
		public int RetryCount { get => historyData.RetryCount; }
		public int ExecutionTime { get => historyData.ExecutionTime; }
		public DateTime Date { get => data.Date; }
	}
}
