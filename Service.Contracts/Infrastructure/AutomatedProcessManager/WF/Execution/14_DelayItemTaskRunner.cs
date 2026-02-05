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
	class DelayItemTaskRunner<TItem> : ITaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private ITaskRunner<TItem> parent;
		private TaskNode<TItem> taskNode;
		private ItemDataModel itemModel;

		public DelayItemTaskRunner(ItemDataModel itemModel)
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

		public string TaskName { get => "DelayItem"; }

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
			itemData.ItemStatus = ItemStatus.Delayed;
			itemData.StatusReason = "Item delayed by workflow definition";
			itemData.DelayedUntil = GetDelayDate(itemData.RetryCount, taskNode.DelayTime);
			await itemModel.SafeUpdate(itemData, CancellationToken.None, itemData.TaskID);
		}

		private DateTime GetDelayDate(int retryCount, TimeSpan? delayTime)
		{
			if (delayTime.HasValue)
			{
				return DateTime.Now.Add(delayTime.Value);
			}
			else
			{
				var seconds = 30 + (120 * retryCount);
				if (seconds > 3600)
					seconds = 3600;
				return DateTime.Now.Add(TimeSpan.FromSeconds(seconds));
			}
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
