using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts.Database;
using Services.Core;

namespace Service.Contracts.WF
{
    abstract class BaseTaskRunner<TItem> : ITaskRunner<TItem>
        where TItem : WorkItem, new()
    {
        protected CancellationTokenSource cts;
        protected ManualResetEvent stopWaitHandle;
        protected ManualResetEvent idleWaitHandle;
        private int workflowid;
        private int taskid;
        private string taskName;

        protected IFactory factory;
        protected ITaskRunner<TItem> parent;
        protected TaskNode<TItem> taskNode;
        protected Func<ItemData, Task> next;
        protected IEventQueue events;
        protected ILogService log;
        protected TaskDataModel taskModel;
        protected ItemDataModel itemModel;
        protected ItemPriorityQueue readyItems;


        public BaseTaskRunner(IFactory factory)
        {
            this.factory = factory;
            events = factory.GetInstance<IEventQueue>();
            log = factory.GetInstance<ILogService>();
            taskModel = factory.GetInstance<TaskDataModel>();
            itemModel = factory.GetInstance<ItemDataModel>();
            readyItems = new ItemPriorityQueue();
            cts = new CancellationTokenSource();
            stopWaitHandle = new ManualResetEvent(false);
            idleWaitHandle = new ManualResetEvent(false);
        }


        public void Dispose()
        {
            cts.Dispose();
            stopWaitHandle.Dispose();
            idleWaitHandle.Dispose();
            factory = null;
            taskNode = null;
            next = null;
            workflowid = 0;
            taskid = 0;
            events = null;
            log = null;
            taskModel = null;
            itemModel = null;
        }


        public int WorkflowID { get => workflowid; }

        public int TaskID { get => taskid; }

        public string TaskName { get => taskName; }

        public Type TaskType { get => taskNode.TaskType; }

        public ITaskRunner<TItem> Parent { get => parent; }


        public IEnumerable<ITaskRunner<TItem>> Tasks
        {
            get
            {
                return Enumerable.Empty<ITaskRunner<TItem>>();
            }
        }


        protected virtual TaskType BindTaskType { get => WF.TaskType.ExecuteTask; }


        public virtual async Task Initialize(ITaskRunner<TItem> parent, TaskNode<TItem> taskNode)
        {
            this.parent = parent;
            this.taskNode = taskNode;
            workflowid = taskNode.WorkflowID;
            var taskData = await taskModel.BindTaskToDatabase(workflowid, taskNode.Name, BindTaskType);
            taskid = taskData.TaskID;
            taskName = taskData.TaskName;
        }

        protected abstract Task ExecuteItems();

        protected abstract Task<TaskResult> ExecuteItemAsync(ItemData itemData, ExecutionMode executionMode, CancellationToken cancellationToken);


        public virtual void Start(Func<ItemData, Task> next)
        {
            this.next = next;
            stopWaitHandle = new ManualResetEvent(false);

            var wfManager = factory.GetInstance<WFManager>();
            var queue = wfManager.GetWorkflowQueue(WorkflowID);
            queue.RegisterForTask(TaskID, (items) => readyItems.Enqueue(items));

            Task.Factory.StartNew(ExecutionLoop, TaskCreationOptions.LongRunning);

            events.Subscribe<WakeWorkflowTaskEvent>((e) =>
            {
                if(e.WorkflowID == workflowid && e.TaskID == taskid)
                    Wake();
            });

            events.Subscribe<ItemPriorityUpdateEvent>((e) =>
            {
                if(e.WorkflowID == workflowid)
                {
                    if(readyItems.TryRemove(i => i.ItemID == e.ItemID, out var item))
                    {
                        item.ItemPriority = e.ItemPriority;
                        readyItems.Enqueue(item);
                    }
                }
            });
        }

        protected async Task ExecutionLoop()
        {
            try
            {
                do
                {
                    try
                    {
                        idleWaitHandle.Reset();
                        if(readyItems.Count > 0)
                        {
                            await ExecuteItems();
                        }
                        else
                        {
                            await idleWaitHandle.WaitOneAsync(1000, cts.Token);
                        }
                    }
                    catch(Exception ex)
                    {
                        // NOTE: We should normally only reach this point if the DB is down, taskModel.ResetActiveItems will internally catch any
                        // error and in case of failure return false. So the following code will keep retrying until either the DB comes back up
                        // or the task is requested to stop.
                        log.LogException("Exception while trying to execute items. Active Items will be reset back to the Delayed state.", ex);
                        await taskModel.ResetTaskItems(WorkflowID, TaskID, cts.Token);
                        await idleWaitHandle.WaitOneAsync(5000, cts.Token);
                    }
                } while(!cts.IsCancellationRequested);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
            }
            finally
            {
                stopWaitHandle.Set();
            }
        }

        // IMPORTANT NOTES
        // There are two possible sources of exceptions inside the ExecuteItems method:
        //
        //		1) The code of the task provided by the developer, which can throw an unhandled exception. So long as the task code does not start additional threads,
        //		   unhandled exceptions will be catched by our try/catch here, and the item will simply be put in the rejected state. However, if the task starts
        //		   other threads (which is not recomended btw) and forgets to synchronize, or handle exceptions correctly, those exceptions will go unhandled. That
        //		   mistake will be the responsability of the developer that created the task, and should not be considered an issue of the Workflow Infrastructure.
        //
        //		2) The second possible source of errors is while we try to update the item state in the database (after the task code has executed). This can happen
        //		   for instance if the database goes down momentarily. In this case we do nothing because the caller will catch the error and enter a loop where it 
        //		   will try to reset all active items back to delayed, that loop will retry as many times as necesary (until the DB is operational again).
        //
        //  Developers creating tasks for the workflow manager need to remember that the same item can be retried many times, for many reasons. So tasks should be
        //  designed as "idempotent", or at the very minimum, the logic in the task should gracefully handle retries without causing errors or creating undesirable
        //	side effects (like creating duplicate records).
        //
        //	Finally if the task is cancelled (IsCancellationRequested returns true) we can simply exit this method and forget the active items that might still be pending to
        //	execute. This is ok because when the task is started again, it will automatically move all Active items to the delayed state, and then retry its execution.
        //
        protected async Task ExecuteItem(ItemData itemData, Func<Task<TaskResult>> executionFunction)
        {
            TaskResult tr;
            Stopwatch sw = new Stopwatch();

            if(cts.IsCancellationRequested)
                return;         // NOTE: We can just exit here, as items left in the active state will be reset when the task is restarted later on.

            // Execute item
            sw.Reset();
            sw.Start();
            try
            {
                itemData.RouteCode = null;
                tr = await executionFunction();
                sw.Stop();
            }
            catch(Exception ex)
            {
                sw.Stop();

                itemData.RetryCount++;

                tr = await InvokeOnExceptionHandlers(itemData, ex);

                // tr.Status being Throw means one of the following possible conditions:
                //		> That no .OnException handler was executed (no matching handler was found)
                //		> That a matching handler was run, but it too throwed an error (in which case the error log 
                //		  entry has already been created in the item log).
                //		> That a matching handler was run, but it returned the Throw status on its own, as it wishes
                //		  to let the exception be handled by the workflow manager.
                //
                // Any other task result means that a handler ran successfully and it wishes the workflow manager
                // to consider the exception as handled.

                if(tr.Status == TaskStatus.Throw)
                {
                    if(parent != null)
                    {
                        var handler = parent.GetExceptionHandler(itemData, ex, false);
                        if(handler != null)
                        {
                            itemData.LastException = itemModel.SerializeException(ex);
                            await itemModel.AddExceptionHistoryEntry(TaskID, TaskName, itemData, ex, (int)sw.ElapsedMilliseconds);
                            await handler.HandleException(itemData, ex);
                            // IMPORTANT: After calling handler.HandleException we should NOT touch this item any more... just continue to next one.
                            return;
                        }
                    }

                    // NOTE: If we reach this point, then no exception handler can manage this error. In that case we 
                    // follow standard procedure, which is:
                    //		> Delay the item if it has not been retried MaxTries times
                    //		> or Reject the item if MaxTries has been reached.

                    if(itemData.RetryCount >= itemData.MaxTries || itemData.RetryCount >= taskNode.Options.MaxRetriesBeforeReject)
                        tr = TaskResult.Reject("Item was Rejected due to an Unhandled Exception", ex);
                    else
                        tr = TaskResult.Delay("Item was Delayed due to an Unhanded Exception", ex, taskNode.Options.RetryDelayTime);
                }
            }

			if (itemData.ItemStatus != ItemStatus.Waiting)
			{
                // Insert task history entry
                await itemModel.AddItemHistoryEntry(TaskID, TaskName, itemData, tr, (int)sw.ElapsedMilliseconds);
                
                // Evaluate the TaskResult generated by the task, but only if the item was not put in the waiting state
                switch(tr.Status)
				{
					case TaskStatus.OK:
						itemData.RouteCode = tr.RouteCode;
						await next(itemData); // In this case the call to next will update the item state, so we can return right away.
						return;
					case TaskStatus.Delayed:
						itemData.DelayedUntil = GetDelayedDate(tr.DelayTime);
						itemData.ItemStatus = ItemStatus.Delayed;
						itemData.StatusReason = tr.Reason;
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
                    case TaskStatus.Wait:
                        if(taskNode.ActionType == WFActionType.WaitTask)
                            await MoveItemToTask(itemData);
                        else
                            throw new InvalidOperationException($"Unimplemented TaskResult: Wait is only valid for WaitingTasks");
                        break;
                    case TaskStatus.ReEnqueue:
						readyItems.ReEnqueue(itemData);
						break;
					default:
						throw new InvalidOperationException($"Unimplemented TaskResult: {tr.Status}");
				}
			}

            // Preserve state changes made to the item...
            await itemModel.SafeUpdate(itemData, cts.Token, itemData.TaskID);
        }

        public virtual void Stop()
        {
            cts.Cancel();
        }


        public virtual async Task<bool> WaitForStop(TimeSpan timeout)
        {
            return await stopWaitHandle.WaitOneAsync(timeout, CancellationToken.None);
        }


        // Moves the item to this task and delays to a past date so it executes as soon as possible
        public virtual async Task MoveItemToTask(ItemData item)
        {
            var oldTaskID = item.TaskID;
            item.TaskID = TaskID;
            item.ItemStatus = ItemStatus.Active;
            item.DelayedUntil = DateTime.Now.AddSeconds(-1);
            item.RetryCount = 0;

            // Ensure item can be moved to this task by calling SafeUpdate
            if(!await itemModel.SafeUpdate(item, cts.Token, oldTaskID))
                return;

            readyItems.Enqueue(item);
            Wake();
        }


        protected void Wake()
        {
            idleWaitHandle.Set();
        }

        // Executes the item in "out of flow" mode. Main differences for out of flow execution are:
        //		> The item is not moved to this task
        //		> The state of the item is not preserved
        //		> It does not matter if the item is actively executing in a different task
        //		> Unhandled exceptions are the responsability of the caller
        //		> And since the real item is not really moved or updated, in theory out of flow execution should not have side effects.
        //
        // On the side effects note: Tasks running in out of flow mode should attempt to eliminate any change at producing undesirable
        // side effects. Not all tasks qualify for that, that is why tasks that can be run out of flow, need to be marked specifically
        // to opt-in into this capability.
        public virtual async Task<TaskResult> OutOfFlowExecute(ItemData item)
        {
            if(!taskNode.CanRunOutOfFlow)
                throw new InvalidOperationException("This task does not allow OutOfFlow execution.");
            TaskResult tr;
            try
            {
                // IMPORTANT: In this case the ItemState should not be updated after execution, just execute, log, and exit
                tr = await ExecuteItemAsync(item, ExecutionMode.OutOfFlow, cts.Token);
                await itemModel.LogMessage(item, TaskName, "Item executed in out of flow mode.", ItemLogVisibility.Restricted);
            }
            catch(Exception ex)
            {
                await itemModel.LogException(item, TaskName, "Item executed in out of flow mode, and generated an exception.", ex, ItemLogVisibility.Restricted);
                tr = TaskResult.Reject("An unhandled exception was thrown by the task.", ex);
            }
            return tr;
        }


        protected async Task<TaskResult> InvokeOnExceptionHandlers(ItemData itemData, Exception exception)
        {
            // Attempts to Execute specific handlers first (regardless of the order in which they were registered in the workflow)
            var especificExceptionHandlers = taskNode.ExceptionHandlers.Where(t => t.ExceptionType != typeof(Exception)).ToList();
            foreach(var exHandler in especificExceptionHandlers)
            {
                if(exHandler.ExceptionType == exception.GetType())
                    return await runHandler(exHandler);
            }

            // If no match, now attempts to execute the first generic handler (a generic handler is one which exception type is 'Exception')
            // Notice that currently we will ignore generic handlers other than the first one (as there is no way we can handle multiple task
            // results comming from multiple handlers).

            var genericExceptionHandler = taskNode.ExceptionHandlers.Where(t => t.ExceptionType == typeof(Exception)).FirstOrDefault();
            if(genericExceptionHandler != null)
                return await runHandler(genericExceptionHandler);

            // If no match, simply return Throw to let the exception be handled normally by the workflow manager
            return TaskResult.Throw();

            async Task<TaskResult> runHandler(TaskDescriptor<TItem> exHandler)
            {
                try
                {
                    TItem itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
                    itemInstance.ExecutionMode = ExecutionMode.InFlow;
                    itemInstance.InitItem(factory, itemData, taskName);
                    var handler = factory.GetInstance(exHandler.TaskType);
                    var task = Reflex.Invoke(handler, "ExecuteAsync", itemInstance, exception, cts.Token) as Task<TaskResult>;
                    await task;
                    await itemModel.LogMessage(itemData, TaskName, $"{exHandler.TaskType.Name} Executed in response to an unhandled exception in Task {taskNode.Name}.", ItemLogVisibility.Restricted);
                    return task.Result;
                }
                catch(Exception ex)
                {
                    await itemModel.LogException(itemData, TaskName, $"{exHandler.TaskType.Name} Executed in response to an unhandled exception in Task {taskNode.Name}, but throwed as well.", ex, ItemLogVisibility.Restricted);
                    return TaskResult.Throw();
                }
            }
        }

        protected DateTime GetDelayedDate(TimeSpan delayTime)
        {
            if(delayTime.TotalSeconds < 5)
                delayTime = TimeSpan.FromSeconds(5);
            if(delayTime.TotalHours > 24)
                delayTime = TimeSpan.FromHours(24);

            return DateTime.Now.Add(delayTime);
        }

        public ITaskExceptionHandler<TItem> GetExceptionHandler(ItemData itemData, Exception ex, bool finallyLocked)
        {
            if(parent != null)
                return parent.GetExceptionHandler(itemData, ex, finallyLocked);
            else
                return null;
        }
    }
}
