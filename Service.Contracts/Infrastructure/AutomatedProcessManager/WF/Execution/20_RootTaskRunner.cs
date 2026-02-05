using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Service.Contracts.Database;

namespace Service.Contracts.WF
{
	class RootTaskRunner<TItem> : ITaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private IFactory factory;
		private TaskNode<TItem> taskNode;
		private List<ITaskRunner<TItem>> tasks;
		private ItemDataModel itemModel;
		private CancellationTokenSource cts;


		public RootTaskRunner(IFactory factory, ItemDataModel itemModel)
		{
			this.factory = factory;
			this.itemModel = itemModel;
			cts = new CancellationTokenSource();
		}

		public void Dispose()
		{
			if (cts != null)
				cts.Dispose();
			factory = null;
			taskNode = null;
			tasks = null;
			itemModel = null;
			cts = null;
		}

		public int WorkflowID { get => taskNode.WorkflowID; }

		public int TaskID { get => 0; }

		public string TaskName { get => taskNode.Name; }

		public Type TaskType { get => null; }

		public ITaskRunner<TItem> Parent { get => null; }

		public IEnumerable<ITaskRunner<TItem>> Tasks
		{
			get => tasks;
		}

		public async Task Initialize(ITaskRunner<TItem> parent, TaskNode<TItem> taskNode)
		{
			this.taskNode = taskNode;
			tasks = await WFRunner<TItem>.CreateTasks(factory, this, taskNode, CompleteItem);
		}


		private async Task CompleteItem(ItemData itemData)
		{
			var oldTaskID = itemData.TaskID;
			itemData.ItemStatus = ItemStatus.Completed;
			itemData.WorkflowStatus = WorkflowStatus.Completed;
			itemData.CompletedFrom = itemData.TaskID;
			itemData.CompletedDate = DateTime.Now;
			itemData.TaskID = null;
			if (await itemModel.SafeUpdate(itemData, cts.Token, oldTaskID))
			{
				await itemModel.LogMessage(itemData, TaskName, $"Item {itemData.ItemID} completed normally.", ItemLogVisibility.Public);
			}
		}


		public async Task MoveItemToTask(ItemData itemData)
		{
			if(tasks.Count > 0)
			{
				await tasks[0].MoveItemToTask(itemData);
			}
			else
			{
				throw new InvalidOperationException("Cannot insert item into this workflow because the workflow is empty");
			}
		}


		public void Start(Func<ItemData, Task> next)
		{
			// NOTE: Empty on purpose
		}

		public void Stop()
		{
			foreach (var task in tasks)
				task.Stop();
			cts.Cancel();
		}

		public async Task<bool> WaitForStop(TimeSpan timeout)
		{
			bool result = true;
			foreach (var task in tasks)
				result &= await task.WaitForStop(timeout);
			return result;
		}

		public Task<TaskResult> OutOfFlowExecute(ItemData item)
		{
			throw new Exception("Cannot execute this Task directly");
		}

		public ITaskExceptionHandler<TItem> GetExceptionHandler(ItemData itemData, Exception ex, bool finallyLocked)
		{
			return null;
		}
	}
}
