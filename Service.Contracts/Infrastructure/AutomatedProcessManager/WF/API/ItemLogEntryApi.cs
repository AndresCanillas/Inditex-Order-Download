using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class ItemLogEntryApi : IItemLogEntry
	{
		private ItemLog data;
		private BaseItemLogData entryData;

		public ItemLogEntryApi(ItemLog data)
		{
			this.data = data;
			this.entryData = JsonConvert.DeserializeObject<BaseItemLogData>(data.AttachedData);
		}

		public long EntryID { get => data.EntryID; }
		public int WorkflowID { get => data.WorkflowID; }
		public long ItemID { get => data.ItemID; }
		public int TaskID { get => entryData.TaskID; }
		public string TaskName { get => entryData.TaskName; }
		public ItemLogEntryType EntryType { get => data.EntryType; }
		public string Message { get => entryData.Message; }
		public string ExceptionType { get => entryData.ExceptionType; }
		public string ExceptionMessage { get => entryData.ExceptionMessage; }
		public string ExceptionStackTrace { get => entryData.ExceptionStackTrace; }
		public DateTime Date { get => data.Date; }
	}
}
