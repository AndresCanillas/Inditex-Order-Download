using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class WorkflowApi : IWorkflow
	{
		private IFactory factory;
		private WorkflowData workflowdata;
		private readonly List<IWorkflowTask> tasks;
		private readonly Dictionary<int, IWorkflowTask> taskIndex1;
		private readonly Dictionary<string, IWorkflowTask> taskIndex2;
		private readonly List<IWorkflowTask> detachedTasks;
		private IEventQueue events;
		private TaskDataModel taskModel;
		private ItemDataModel itemModel;

		public WorkflowApi(IFactory factory, IEventQueue events)
		{
			this.factory = factory;
			this.events = events;
			taskModel = factory.GetInstance<TaskDataModel>();
			itemModel = factory.GetInstance<ItemDataModel>();
			tasks = new List<IWorkflowTask>();
			taskIndex1 = new Dictionary<int, IWorkflowTask>();
			taskIndex2 = new Dictionary<string, IWorkflowTask>();
			detachedTasks = new List<IWorkflowTask>();
		}

		internal async Task Initialize(WorkflowData workflowdata)
		{
			this.workflowdata = workflowdata;
			var list = await taskModel.GetWorkflowTasks(workflowdata.WorkflowID);
			foreach (var taskData in list)
			{
				var taskApi = factory.GetInstance<WorkflowTaskApi>();
				taskApi.Initialize(taskData);
				if (taskData.Detached)
				{
					detachedTasks.Add(taskApi);
				}
				else
				{
					tasks.Add(taskApi);
					taskIndex1.Add(taskData.TaskID, taskApi);
					taskIndex2.Add(taskData.TaskName, taskApi);
				}
			}
		}

		public int WorkflowID { get => workflowdata.WorkflowID; }

		public string Name { get => workflowdata.Name; }

		public IEnumerable<IWorkflowTask> GetTasks()
		{
			foreach (var taskApi in tasks)
				yield return taskApi;
		}

		public IWorkflowTask GetTask(string name)
		{
			if (!taskIndex2.TryGetValue(name, out var taskApi))
				throw new InvalidOperationException($"Task {name} is not defined in this workflow.");
			return taskApi;
		}

		public IWorkflowTask GetTask(int taskid)
		{
			if (!taskIndex1.TryGetValue(taskid, out var taskApi))
				throw new InvalidOperationException($"Task with ID {taskid} is not defined in this workflow.");
			return taskApi;
		}

		public IWorkflowTask GetTask<T>()
		{
			var taskName = typeof(T).Name;
			if (!taskIndex2.TryGetValue(taskName, out var taskApi))
				throw new InvalidOperationException($"Task {taskName} is not defined in this workflow.");
			return taskApi;
		}

		public IEnumerable<IWorkflowTask> GetDetachedTasks()
		{
			foreach (var taskApi in detachedTasks)
				yield return taskApi;
		}

		public async Task DeleteDetachedTaskAsync(int taskid)
		{
			var taskApi = detachedTasks.FirstOrDefault(t => t.TaskID == taskid);
			if (taskApi != null)
			{
				await taskModel.DeleteDetachedTask(WorkflowID, taskid);
				detachedTasks.Remove(taskApi);
			}
			else throw new InvalidOperationException($"Task with ID {taskid} does not exist or is not detached.");
		}

		public async Task<CounterData> GetItemCountersAsync()
		{
			var counterData = await itemModel.GetWorkflowCounters(WorkflowID);
			return counterData;
		}

		public async Task<Dictionary<int, TaskCounterData>> GetItemCountersByTaskAsync()
		{
			var counters = await itemModel.GetWorkflowCountersByTaskAsync(WorkflowID);
			return CreateCountersByTaskDictionary(counters);
		}

		public async Task<IWorkItem> FindItemAsync(long itemid)
		{
			var itemData = await itemModel.GetItemByID(WorkflowID, itemid);
			if (itemData != null)
			{
				var itemApi = factory.GetInstance<WorkItemApi>();
				itemApi.Initialize(itemData);
				return itemApi;
			}
			return null;
		}

		public async Task<IWorkItem> FindItemAsync(string itemName)
		{
			var itemData = await itemModel.GetItemByName(WorkflowID, itemName);
			if (itemData != null)
			{
				var itemApi = factory.GetInstance<WorkItemApi>();
				itemApi.Initialize(itemData);
				return itemApi;
			}
			return null;
		}

		public async Task<IEnumerable<IWorkItem>> FindItemsAsync(string itemName, string keywords, DateTime? fromdate, DateTime? todate)
		{
			var items = await itemModel.FindItems(WorkflowID, itemName, keywords, fromdate, todate);
			var result = new List<IWorkItem>(items.Count);
			foreach (var itemData in items)
			{
				var itemApi = factory.GetInstance<WorkItemApi>();
				itemApi.Initialize(itemData);
				result.Add(itemApi);
			}
			return result;
		}

		public async Task<IEnumerable<IWorkItem>> FindItemsAsync(int? taskid, ItemStatus? itemstatus, long? itemid, string itemName, 
			string keywords, DateTime? fromdate, DateTime? todate)
		{
			var items = await itemModel.FindItems(WorkflowID, taskid, itemstatus, itemid, itemName, keywords, fromdate, todate);
			var result = new List<IWorkItem>(items.Count);
			foreach (var itemData in items)
			{
				var itemApi = factory.GetInstance<WorkItemApi>();
				itemApi.Initialize(itemData);
				result.Add(itemApi);
			}
			return result;
		}

		public async Task InsertItemAsync<TItem>(TItem item)
			where TItem : WorkItem, new()
		{
			var wfManager = factory.GetInstance<WFManager>();
			var runner = wfManager.GetRunner<TItem>(WorkflowID);
			if (runner != null)
				await runner.InsertItem(item);
			else
				await CreateItemInRemoteWorkflow(item);
		}


		public async Task InsertItemAsync<TItem>(TItem item, int? taskid = null, ItemStatus? itemStatus = null, bool remoteWorkflow = false)
			where TItem : WorkItem, new()
		{
			var wfManager = factory.GetInstance<WFManager>();
			var runner = wfManager.GetRunner<TItem>(WorkflowID);
			if(runner != null && taskid == null && itemStatus == null && !remoteWorkflow)
				await runner.InsertItem(item);
			else
				await CreateItemInRemoteWorkflow(item, taskid, itemStatus, remoteWorkflow);
		}

		private async Task CreateItemInRemoteWorkflow<TItem>(TItem item, int? taskid = null, ItemStatus? itemStatus = null, bool remoteWorkflow = false)
			where TItem : WorkItem, new()
		{
            TaskData firstTask;

            if(taskid != null)
            {
                firstTask = await taskModel.GetTaskByTaskID(WorkflowID, taskid.Value);
                if(firstTask == null)
                    throw new InvalidOperationException($"TaskID {taskid} is not a valid task for workflow {Name}");
            }
            else
            {
                firstTask = await taskModel.GetInitialTask(WorkflowID);
            }
            
            var itemModel = factory.GetInstance<ItemDataModel>();
			var itemData = await itemModel.CreateItemForWorkflow(WorkflowID, item, taskid, itemStatus);
			itemData.TaskID = firstTask.TaskID;
			itemData.ItemStatus = itemStatus ?? ItemStatus.Delayed;
		    itemData.DelayedUntil = DateTime.Now.AddSeconds(-1);
			itemData.RetryCount = 0;
			await itemModel.Update(itemData, CancellationToken.None);
			item.InitItem(factory, itemData, firstTask.TaskName);
		}


		public async Task DelayAsync(IWorkItem item, string reason, TimeSpan delayTime, IIdentity identity)
		{
			var itemApi = item as WorkItemApi;
			if (itemApi == null)
				throw new InvalidOperationException("The given argument 'item' is invalid.");

			if (!item.TaskID.HasValue)
				throw new InvalidOperationException("The item is not currently InFlow");

			if (item.ItemStatus == ItemStatus.Active)
				throw new InvalidOperationException("The item is Active, cannot update its state while it is executing.");

			if (String.IsNullOrWhiteSpace(reason))
				throw new InvalidOperationException("Need to provide a reason why the item is being delayed.");

			if (delayTime.TotalMinutes < 1 || delayTime.TotalHours > 24)
				throw new InvalidOperationException("Delay time must be in a valid range of 1 minute up to 1 day.");

			await itemModel.MakeDelayedInTask(itemApi.itemData, reason, delayTime, identity);
		}


		public async Task MakeActiveAsync(IWorkItem item, IIdentity identity = null)
		{
			var itemApi = item as WorkItemApi;
			if (itemApi == null)
				throw new InvalidOperationException("The given argument 'item' is invalid.");

			if (!itemApi.TaskID.HasValue)
				throw new InvalidOperationException("The item is not currently InFlow");

			if (item.ItemStatus == ItemStatus.Active)
				throw new InvalidOperationException("The item is Active, cannot update its state while it is executing.");

			await itemModel.MakeActiveInTask(item.TaskID.Value, itemApi.itemData, identity);
			events.Send(new WakeWorkflowTaskEvent(item.WorkflowID, item.TaskID.Value));
		}


		public async Task RejectAsync(IWorkItem item, string reason, IIdentity identity)
		{
			var itemApi = item as WorkItemApi;
			if (itemApi == null)
				throw new InvalidOperationException("The given argument 'item' is invalid.");

			if (!item.TaskID.HasValue)
				throw new InvalidOperationException("The item is not currently InFlow");

			if (item.ItemStatus == ItemStatus.Active)
				throw new InvalidOperationException("The item is Active, cannot update its state while it is executing.");

			if (String.IsNullOrWhiteSpace(reason))
				throw new InvalidOperationException("Need to provide a reason why the item is being rejected.");

			await itemModel.MakeRejectedInTask(itemApi.itemData, reason, identity);
		}


		public async Task CompleteAsync(IWorkItem item, string reason, IIdentity identity)
		{
			await itemModel.CompleteItem(WorkflowID, item.ItemID, reason, ItemStatus.Completed, identity);
		}


		public async Task CancelAsync(IWorkItem item, string reason, IIdentity identity)
		{
			await itemModel.CompleteItem(WorkflowID, item.ItemID, reason, ItemStatus.Cancelled, identity);
		}


		public async Task MoveAsync(IWorkItem item, int taskid, ItemStatus status, TimeSpan? delayTime, string reason, IIdentity identity)
		{
			await itemModel.MoveItem(WorkflowID, item.ItemID, taskid, status, delayTime, reason, identity, false);
		}

		public async Task<bool> CanMoveAsync(IWorkItem item)
		{
			return await itemModel.CanMoveItem(WorkflowID, item.ItemID);
		}

		public async Task ChangePriorityAsync(IWorkItem item, ItemPriority priority, IIdentity identity)
		{
			events.Send(new ItemPriorityUpdateEvent()
			{
				WorkflowID = item.WorkflowID,
				ItemID = item.ItemID,
				ItemPriority = priority,
			});

			await itemModel.ChangeItemPriority(WorkflowID, item.ItemID, priority, identity);

		}

		public async Task ReactivateAsync(IWorkItem item, int? taskid, ItemStatus status, TimeSpan? delayTime, string reason, IIdentity identity)
		{
			await itemModel.MoveItem(WorkflowID, item.ItemID, taskid, status, delayTime, reason, identity, true);
		}


		public async Task<IWorkItem> WaitForItemStatus(long itemid, ItemStatus expectedItemStatus)
		{
			return await WaitForItemStatus(itemid, expectedItemStatus, TimeSpan.FromDays(365));
		}


		public async Task<IWorkItem> WaitForItemStatus(long itemid, ItemStatus expectedItemStatus, TimeSpan timeout)
		{
			IWorkItem itemApi;
			var timeoutDate = DateTime.Now.Add(timeout);
			int expectedFlags = 0;
			do
			{
				do
				{
					itemApi = await FindItemAsync(itemid);
					if (itemApi == null)
						throw new Exception($"Item {itemid} could not be found.");
					expectedFlags = ((int)itemApi.ItemStatus) & ((int)expectedItemStatus);
					if (expectedFlags == 0)
					{
						if (DateTime.Now > timeoutDate)
							throw new TimeoutException("Item did not enter the expected status within the specified timeout");
						await Task.Delay(500);
					}
				} while (expectedFlags == 0);
			} while (expectedItemStatus == ItemStatus.Delayed && itemApi.DelayedUntil < DateTime.Now); // In case of delayed status we also need the DelayedUntil date to be a future date... otherwise keep waiting.
			return itemApi;
		}

		public async Task CancelAllItemsAsync(string reason)
		{
			await itemModel.CancelAllItemsAsync(WorkflowID, reason);
		}

		internal static Dictionary<int, TaskCounterData> CreateCountersByTaskDictionary(List<TaskItemCounter> counters)
		{
			TaskCounterData taskInfo;
			var index = new Dictionary<int, TaskCounterData>();

			foreach (var c in counters)
			{
				if (!index.TryGetValue(c.TaskID, out taskInfo))
				{
					taskInfo = new TaskCounterData() { TaskID = c.TaskID };
					index.Add(c.TaskID, taskInfo);
				}
				
				taskInfo.AddCounter(c);
			}

			return index;
		}

		internal static Dictionary<string, StateCounterData> CreateCountersByStateDictionary(List<StateItemCounter> counters)
		{
			StateCounterData stateInfo;
			var index = new Dictionary<string, StateCounterData>();

			foreach (var c in counters)
			{
				if (!index.TryGetValue(c.Value, out stateInfo))
				{
					stateInfo = new StateCounterData() { Value = c.Value };
					index.Add(c.Value, stateInfo);
				}

				stateInfo.AddCounter(c);
			}

			return index;
		}
	}
}
