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
	class ActionTaskRunner<TItem> : ITaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private IFactory factory;
		private ITaskRunner<TItem> parent;
		private TaskNode<TItem> taskNode;
		private ItemDataModel itemModel;
		private Func<ItemData, Task> next;

		public ActionTaskRunner(IFactory factory, ItemDataModel itemModel)
		{
			this.factory = factory;
			this.itemModel = itemModel;
		}

		public void Dispose()
		{
			parent = null;
			taskNode = null;
		}

		public int WorkflowID { get => taskNode.WorkflowID; }

		public int TaskID { get => 0; }

		public string TaskName { get => "Action"; }

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
			TaskResult tr = TaskResult.OK();

			try
			{
				if (!await itemModel.SafeUpdate(itemData, CancellationToken.None, itemData.TaskID))
					return; 
				
				TItem itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
				itemInstance.ExecutionMode = ExecutionMode.InFlow;
				itemInstance.InitItem(factory, itemData, TaskName);
				taskNode.Callback(itemInstance);
				itemData.ItemState = JsonConvert.SerializeObject(itemInstance);

				await itemModel.LogMessage(itemData, TaskName, $"Action executed", ItemLogVisibility.Public);
				await next(itemData);
			}
			catch (Exception ex)
			{
				itemData.RetryCount++;

				// IMPORTANT: After calling handler.HandleException we should NOT touch this item any more... just exit.
				if (parent != null)
				{
					var handler = parent.GetExceptionHandler(itemData, ex, false);
					if (handler != null)
					{
						await itemModel.LogException(itemData, TaskName, "Exception while executing Action", ex, ItemLogVisibility.Public);
						await handler.HandleException(itemData, ex);
						return;  // At this point the exception is considered as handled, and there should be no need to make any item state changes, so continue executing next item...
					}
				}

				// NOTE: If we reach this point, then no exception handler can manage this error. In that case we follow standard procedure: Either delay or reject the item based on its RetryCount.
				var delayInMinutes = itemData.RetryCount * 5;
				if (itemData.RetryCount < itemData.MaxTries)
				{
					itemData.ItemStatus = ItemStatus.Delayed;
					itemData.StatusReason = "Item was Delayed due to an Unhandled Exception";
					tr = TaskResult.Delay(itemData.StatusReason, ex, TimeSpan.FromMinutes(delayInMinutes));
				}
				else
				{
					itemData.ItemStatus = ItemStatus.Rejected;
					itemData.StatusReason = "Item was Rejected due to an Unhandled Exception";
					tr = TaskResult.Reject(itemData.StatusReason, ex);
				}

				// update item state in the database
				await itemModel.SafeUpdate(itemData, CancellationToken.None, itemData.TaskID);
				await itemModel.LogException(itemData, TaskName, "Exception while executing Action", ex, ItemLogVisibility.Public);
			}
		}

		public void Start(Func<ItemData, Task> next)
		{
			this.next = next;
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
