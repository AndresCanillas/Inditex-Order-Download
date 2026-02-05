using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class WorkItemApi : IWorkItem
	{
		internal ItemData itemData;
		private ItemDataModel itemModel;
		private IEventQueue events;

		private string itemName;
		private string keywords;
		private ItemPriority itemPriority;

		public int WorkflowID { get => itemData.WorkflowID; }
		public long ItemID { get => itemData.ItemID; }
		public string Name { get => itemName; set => itemName = value; }
		public string Keywords { get => keywords; set => keywords = value; }
		public int? TaskID { get => itemData.TaskID; }
		public DateTime? TaskDate { get => itemData.TaskDate; }
		public int RetryCount { get => itemData.RetryCount; }
		public ItemPriority ItemPriority { get => itemPriority; set => itemPriority = value; }
		public ItemStatus ItemStatus { get => itemData.ItemStatus; }
		public string StatusReason { get => itemData.StatusReason; }
		public DateTime DelayedUntil { get => itemData.DelayedUntil; }
		public string RouteCode { get => itemData.RouteCode; }
		public bool Active { get => itemData.ItemStatus == ItemStatus.Active; }
		public WorkflowStatus WorkflowStatus { get => itemData.WorkflowStatus; }
		public int? CompletedFrom { get => itemData.CompletedFrom; }
		public DateTime? CompletedDate { get => itemData.CompletedDate; }

		public WorkItemApi(ItemDataModel itemModel, IEventQueue events)
		{
			this.itemModel = itemModel;
			this.events = events;
		}

		internal void Initialize(ItemData itemData)
		{
			this.itemData = itemData;
			itemName = itemData.ItemName;
			keywords = itemData.Keywords;
			itemPriority = itemData.ItemPriority;
		}


		public async Task<string> GetSavedStateAsync()
		{
			return await itemModel.GetItemState(itemData.WorkflowID, itemData.ItemID);
		}


		public async Task<T> GetSavedStateAsync<T>() where T : WorkItem
		{
			var state = await itemModel.GetItemState(itemData.WorkflowID, itemData.ItemID);
			return JsonConvert.DeserializeObject<T>(state);
		}


		/// <summary>
		/// Updates the saved state of the item (its user defined properties).
		/// </summary>
		/// <param name="state">A json string containing the item state</param>
		/// <remarks>
		/// Update the item state with case, as if the specified json string is invalid, the item might become invalid and will be rejected until the problem is solved.
		/// </remarks>
		public async Task UpdateSavedStateAsync(string state)
		{
			await itemModel.UpdateItemState(itemData.WorkflowID, itemData.ItemID, state);
		}


		public async Task UpdateSavedStateAsync<T>(T state) where T: WorkItem
		{
			if (state == null)
				throw new ArgumentNullException(nameof(state));

			var json = JsonConvert.SerializeObject(state);
			await itemModel.UpdateItemState(itemData.WorkflowID, itemData.ItemID, json);
		}


		public async Task<IEnumerable<IItemHistoryEntry>> GetHistoryAsync()
		{
			var log = await itemModel.GetItemLog(itemData, ItemLogEntryType.TaskHistory);
			var result = new List<ItemHistoryEntryApi>(log.Count);
			foreach(var entry in log)
			{
				result.Add(new ItemHistoryEntryApi(entry));
			}
			return result;
		}

		public async Task<IEnumerable<IItemLogEntry>> GetLogAsync()
		{
			var log = await itemModel.GetItemLog(itemData, null);
			var result = new List<IItemLogEntry>(log.Count);
			foreach (var entry in log)
			{
				if(entry.EntryType == ItemLogEntryType.TaskHistory)
					result.Add(new ItemHistoryEntryApi(entry));
				else
					result.Add(new ItemLogEntryApi(entry));
			}
			return result;
		}

		public async Task<Exception> GetLastExceptionAsync()
		{
			return await itemModel.GetLastException(itemData);
		}

		public async Task SaveAsync()
		{
            var oldPriority = itemData.ItemPriority;
			await itemModel.UpdateEditableProperties(itemData, itemName, keywords, itemPriority);
			itemData.ItemName = itemName;
			itemData.Keywords = keywords;
			itemData.ItemPriority = itemPriority;

            if(oldPriority != itemPriority)
            {
                events.Send(new ItemPriorityUpdateEvent()
                {
                    WorkflowID = itemData.WorkflowID,
                    ItemID = itemData.ItemID,
                    ItemPriority = itemData.ItemPriority,
                });
            }
        }
    }
}
