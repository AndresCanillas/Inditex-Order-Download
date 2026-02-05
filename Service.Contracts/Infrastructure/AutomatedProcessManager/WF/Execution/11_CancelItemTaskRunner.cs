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
	class CancelItemTaskRunner<TItem> : ITaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private ITaskRunner<TItem> parent;
		private TaskNode<TItem> taskNode;
		private ItemDataModel itemModel;

		public CancelItemTaskRunner(ItemDataModel itemModel)
		{
			this.itemModel = itemModel;
		}

		public void Dispose()
		{
			parent = null;
			taskNode = null;
		}

		public int WorkflowID { get => taskNode.WorkflowID; }

		public int TaskID { get => 0; }

		public string TaskName { get => "CancelItem"; }

		public Type TaskType { get => null; }

		public ITaskRunner<TItem> Parent { get => parent; }

		public IEnumerable<ITaskRunner<TItem>> Tasks
		{
			get => Enumerable.Empty<ITaskRunner<TItem>>();
		}

		public Task Initialize(ITaskRunner<TItem> parent, TaskNode<TItem> taskNode)
		{
			this.parent = parent;
			this.taskNode = taskNode;
			return Task.CompletedTask;
		}

		public async Task MoveItemToTask(ItemData itemData)
		{
			var oldTaskID = itemData.TaskID;
			itemData.ItemStatus = ItemStatus.Cancelled;
			itemData.WorkflowStatus = WorkflowStatus.Cancelled;
			itemData.CompletedFrom = itemData.TaskID;
			itemData.CompletedDate = DateTime.Now;
			itemData.TaskID = null;

			// Make sure the item is in the expected task by calling SafeUpdate
			await itemModel.SafeUpdate(itemData, CancellationToken.None, oldTaskID);
		}


		public void Start(Func<ItemData, Task> next)
		{
			// NOTE: Empty on purpose
		}

		public void Stop()
		{
			// NOTE: Empty on purpose
		}

		public Task<bool> WaitForStop(TimeSpan timeout)
		{
			return Task.FromResult(true);
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
