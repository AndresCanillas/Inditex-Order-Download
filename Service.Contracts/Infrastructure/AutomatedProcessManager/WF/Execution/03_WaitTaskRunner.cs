using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class WaitTaskRunner<TEvent, TItem> : SingleTaskRunner<TItem>
		where TEvent : EQEventInfo
		where TItem : WorkItem, new()
	{
		protected WaitingTask<TEvent, TItem> taskInstance;
		private Timer wakeTimeoutTimer;

		public WaitTaskRunner(IFactory factory)
			: base(factory)
		{
		}

		protected override TaskType BindTaskType { get => WF.TaskType.WaitTask; }

		public override void Start(Func<ItemData, Task> next)
		{
			taskInstance = factory.GetInstance(taskNode.TaskType) as WaitingTask<TEvent, TItem>;
			events.OnEventRegistered += HandleEvent;
			base.Start(next);
			Task.Factory.StartNew(WakeByTimeoutLoop, TaskCreationOptions.LongRunning);
		}

		public override void Stop()
		{
			events.OnEventRegistered -= HandleEvent;
			if (wakeTimeoutTimer != null)
			{
				wakeTimeoutTimer.Dispose();
				wakeTimeoutTimer = null;
			}
			base.Stop();
		}

		public override async Task MoveItemToTask(ItemData itemData)
		{
			Exception catchedException = null;
			var oldTaskID = itemData.TaskID;
			itemData.TaskID = TaskID;
			itemData.RetryCount = 0;

			// Make sure the item can be moved to this task by calling SafeUpdate before proceeding
			if (!await itemModel.SafeUpdate(itemData, cts.Token, oldTaskID))
				return;

			// Execute the WaitTask.BeforeWaitingAsync method
			TItem itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
			itemInstance.ExecutionMode = ExecutionMode.InFlow;
			itemInstance.InitItem(factory, itemData, TaskName);
			TaskResult tr;
			var sw = new Stopwatch();
			sw.Start();
			try
			{
				tr = await taskInstance.BeforeWaitingAsync(itemInstance);
				itemData.ItemState = JsonConvert.SerializeObject(itemInstance);
			}
			catch (Exception ex)
			{
				// IMPORTANT: After calling handler.HandleException we should NOT touch this item any more... just continue to next one.
				if (parent != null)
				{
					var handler = parent.GetExceptionHandler(itemData, ex, false);
					if (handler != null)
					{
						await itemModel.AddExceptionHistoryEntry(TaskID, TaskName, itemData, ex, (int)sw.ElapsedMilliseconds);
						await handler.HandleException(itemData, ex);
						return;
					}
				}

				catchedException = ex;

				// NOTE: If we reach this point, then no exception handler can manage this error. In that case we reject the item.
				tr = TaskResult.Reject($"Item was Rejected due to an Unhandled Exception within {TaskName}.BeforeWaiting method.", ex);
			}
			sw.Stop();

			// Insert task history entry
			await itemModel.AddItemHistoryEntry(TaskID, $"{TaskName}.BeforeWaiting", itemData, tr, (int)sw.ElapsedMilliseconds);

			if (taskNode.HasWakeTimeout)
				itemData.WakeTimeout = DateTime.Now.Add(taskNode.WakeTimeout);
			else
				itemData.WakeTimeout = null;

			switch (tr.Status)
			{
				case TaskStatus.OK:
				case TaskStatus.Delayed:
				case TaskStatus.Wait:
					itemData.ItemStatus = ItemStatus.Waiting;
					break;
				case TaskStatus.Rejected:
					itemData.ItemStatus = ItemStatus.Rejected;
					itemData.StatusReason = tr.Reason;
					break;
				case TaskStatus.Completed:
					itemData.ItemStatus = ItemStatus.Completed;
					itemData.StatusReason = tr.Reason;
					itemData.WorkflowStatus = WorkflowStatus.ForcedCompleted;
					itemData.CompletedFrom = TaskID;
					itemData.CompletedDate = DateTime.Now;
					itemData.TaskID = null;
					break;
				case TaskStatus.Cancelled:
					itemData.ItemStatus = ItemStatus.Cancelled;
					itemData.StatusReason = tr.Reason;
					itemData.WorkflowStatus = WorkflowStatus.Cancelled;
					itemData.CompletedFrom = TaskID;
					itemData.CompletedDate = DateTime.Now;
					itemData.TaskID = null;
					break;
                case TaskStatus.SkipWait:
                    await next(itemData);
                    return;
			}

			await itemModel.SafeUpdate(itemData, cts.Token, itemData.TaskID);

			if (itemData.ItemStatus == ItemStatus.Rejected && catchedException != null)
				await InvokeOnExceptionHandlers(itemData, catchedException);
		}

        private void HandleEvent(EQEventInfo e)
        {
            if(e.GetType() == typeof(TEvent))
            {
                var waitingItems = itemModel.WakeWaitingItemsByEvent(WorkflowID, TaskID, taskNode.WakeExpression, e).Result;
                if(waitingItems > 0)
                    Wake();  // <--- This wakes the task runner thread so it processes ready items right away
            }
        }

		protected override async Task<TaskResult> ExecuteItemAsync(ItemData itemData, ExecutionMode executionMode, CancellationToken cancellationToken)
		{
			TItem itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
			itemInstance.ExecutionMode = executionMode;
			itemInstance.InitItem(factory, itemData, TaskName);
			try
			{
				if (itemData.WakeEventState == null)
				{
					var result = await taskInstance.ItemTimeoutAsync(itemInstance, cancellationToken);
					return result;
				}
				else
				{
					var eventInstance = JsonConvert.DeserializeObject<TEvent>(itemData.WakeEventState);
					var result = await taskInstance.ItemAwakeAsync(eventInstance, itemInstance, cancellationToken);
					return result;
				}
			}
			finally
			{
				itemData.ItemState = JsonConvert.SerializeObject(itemInstance);
			}
		}

        private async Task WakeByTimeoutLoop()
        {
            while(!cts.IsCancellationRequested)
            {
                TimeSpan nextTimeout = TimeSpan.FromMinutes(1);
                try
                {
                    if(await itemModel.WakeWaitingItemsByTimeout(WorkflowID, TaskID) > 0)
                        Wake();  // <--- This wakes the task runner thread so it processes ready items right away

                    nextTimeout = await itemModel.GetWaitTaskTimeout(WorkflowID, TaskID, taskNode.WakeTimeout);
                }
                catch(Exception ex)
                {
                    log.LogException(ex);
                }
                await cts.Token.WaitHandle.WaitOneAsync(nextTimeout, CancellationToken.None);
            }
        }
    }
}
