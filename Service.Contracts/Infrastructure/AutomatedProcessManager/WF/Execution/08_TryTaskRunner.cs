using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Service.Contracts.WF
{
	class TryTaskRunner<TItem> : ITaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private IFactory factory;
		private ITaskRunner<TItem> parent;
		private TaskNode<TItem> taskNode;
        private Func<ItemData, Task> next;
        private List<ITaskRunner<TItem>> tryBody;
		private Dictionary<Type, List<ITaskRunner<TItem>>> catchBranches;
		private List<ITaskRunner<TItem>> finallyBody;
		private ItemDataModel itemModel;
		private CancellationTokenSource cts;

		public TryTaskRunner(IFactory factory)
		{
			this.factory = factory;
			itemModel = factory.GetInstance<ItemDataModel>();
			catchBranches = new Dictionary<Type, List<ITaskRunner<TItem>>>();
			cts = new CancellationTokenSource();
		}

		public void Dispose()
		{
			factory = null;
			taskNode = null;
			next = null;
			if (tryBody != null)
			{
				foreach (var task in tryBody)
					task.Dispose();
				tryBody.Clear();
			}
			if (finallyBody != null)
			{
				foreach (var task in finallyBody)
					task.Dispose();
				finallyBody.Clear();
			}
			if (catchBranches != null)
			{
				foreach (var taskList in catchBranches.Values)
				{
					foreach (var task in taskList)
						task.Dispose();
					taskList.Clear();
				}
				catchBranches.Clear();
			}
			if(cts != null)
			{
				cts.Dispose();
			}
			tryBody = null;
			finallyBody = null;
			catchBranches = null;
			cts = null;
			itemModel = null;
		}

		public int WorkflowID { get => taskNode.WorkflowID; }

		public int TaskID { get => 0; }

		public string TaskName { get => "Try"; }

		public Type TaskType { get => null; }

		public ITaskRunner<TItem> Parent { get => parent; }

		public IEnumerable<ITaskRunner<TItem>> Tasks
		{
			get
			{
				if(tryBody != null)
				{
					foreach (var task in tryBody)
						yield return task;
				}
				if (catchBranches != null)
				{
					foreach (var taskList in catchBranches.Values)
					{
						foreach (var task in taskList)
							yield return task;
					}
				}
				if(finallyBody != null)
				{
					foreach (var task in finallyBody)
						yield return task;
				}
			}
		}


		public async Task Initialize(ITaskRunner<TItem> parent, TaskNode<TItem> taskNode)
		{
			this.parent = parent;
			this.taskNode = taskNode;
			List<ITaskRunner<TItem>> branchTasks;
			foreach (var node in taskNode.Children)
			{
				node.WorkflowID = WorkflowID;
				switch (node.NodeType)
				{
					case WFNodeType.TryBody:
						branchTasks = await WFRunner<TItem>.CreateTasks(factory, this, node, MoveToFinallyOrNextTask);
						tryBody = branchTasks;
						break;
					case WFNodeType.CatchBody:
						branchTasks = await WFRunner<TItem>.CreateTasks(factory, this, node, MoveToFinallyOrNextTask);
						catchBranches.Add(node.ExceptionType, branchTasks);
						break;
					case WFNodeType.FinallyBody:
						branchTasks = await WFRunner<TItem>.CreateTasks(factory, this, node, MoveToNextTask);
						finallyBody = branchTasks;
						break;
				}
			}
		}

		private async Task MoveToFinallyOrNextTask(ItemData item)
		{
			if(finallyBody != null && finallyBody.Count > 0)
			{
                // Moves the item to the first task in the finally body
                await finallyBody[0].MoveItemToTask(item);
			}
			else
			{
				if (item.RejectOnFinally)
				{
					await RejectItem(item);
				}
				else if (item.OutstandingHandler.HasValue)
				{
					ITaskRunner<TItem> targetTask = this.FindTaskRunner(item.OutstandingHandler.Value);
					item.OutstandingHandler = null;
					await targetTask.MoveItemToTask(item);
				}
				else await next(item);
			}
		}

		private async Task MoveToNextTask(ItemData item)
		{
			if (item.RejectOnFinally)
			{
				await RejectItem(item);
			}
			else if (item.OutstandingHandler.HasValue)
			{
				ITaskRunner<TItem> targetTask = this.FindTaskRunner(item.OutstandingHandler.Value);
				item.OutstandingHandler = null;
				await targetTask.MoveItemToTask(item);
			}
			else await next(item);
		}

		private async Task RejectItem(ItemData itemData)
		{
			itemData.RejectOnFinally = false; // clear flag out
			itemData.ItemStatus = ItemStatus.Rejected;
			itemData.StatusReason = "Outstanding Exception was not handled";
			await itemModel.SafeUpdate(itemData, cts.Token, itemData.TaskID);
		}

		public async Task MoveItemToTask(ItemData item)
		{
			if(tryBody.Count > 0)
			{
                // Moves the item to the first task in the try body
                await tryBody[0].MoveItemToTask(item);
			}
			else
			{
				// In case of an empty body, just move the item to the finally block (or if there is no finally, to the next task)
				await MoveToFinallyOrNextTask(item);
			}
		}

		public void Start(Func<ItemData, Task> next)
		{
			this.next = next;
		}


		public void Stop()
		{
			if (tryBody != null)
			{
				foreach (var task in tryBody)
					task.Stop();
			}
			if (finallyBody != null)
			{
				foreach (var task in finallyBody)
					task.Stop();
			}
			if (catchBranches != null)
			{
				foreach (var taskList in catchBranches.Values)
				{
					foreach (var task in taskList)
						task.Stop();
				}
			}
			cts.Cancel();
		}


		public async Task<bool> WaitForStop(TimeSpan timeout)
		{
			bool result = true;
			if (tryBody != null)
			{
				foreach (var task in tryBody)
					result &= await task.WaitForStop(timeout);
			}
			if (finallyBody != null)
			{
				foreach (var task in finallyBody)
					result &= await task.WaitForStop(timeout);
			}
			if (catchBranches != null)
			{
				foreach (var taskList in catchBranches.Values)
				{
					foreach (var task in taskList)
						result &= await task.WaitForStop(timeout);
				}
			}
			return result;
		}


		public Task<TaskResult> OutOfFlowExecute(ItemData item)
		{
			throw new Exception("Cannot execute a Try Task directly");
		}


		public ITaskExceptionHandler<TItem> GetExceptionHandler(ItemData itemData, Exception ex, bool finallyLocked)
		{
			bool ItemInFinally = ItemInBranch(itemData, finallyBody);
			if (catchBranches.TryGetValue(ex.GetType(), out var catchBranch) && !ItemInFinally && !ItemInBranch(itemData, catchBranch))
			{
				return new TaskExceptionHandler<TItem>(catchBranch[0]);
			}
			else if (catchBranches.TryGetValue(typeof(Exception), out catchBranch) && !ItemInFinally && !ItemInBranch(itemData, catchBranch))
			{
				return new TaskExceptionHandler<TItem>(catchBranch[0]);
			}
			else if (parent != null)
			{
				var claimedFinallyLock = !finallyLocked && finallyBody != null && !ItemInFinally;
				if (claimedFinallyLock)
					finallyLocked = true;

				var handler = parent.GetExceptionHandler(itemData, ex, finallyLocked);
				if(handler != null)
				{
					if (claimedFinallyLock)
					{
						return new FinallyAndCatchExceptionHandler<TItem>(itemModel,  finallyBody[0], handler.TaskID);
					}
					else
					{
						return handler;
					}
				}
				else
				{
					if (claimedFinallyLock)
						return new FinallyExceptionHandler<TItem>(finallyBody[0]);
				}
				return null;
			}
			else if (!finallyLocked && finallyBody != null && !ItemInFinally)
			{
				return new FinallyExceptionHandler<TItem>(finallyBody[0]);
			}
			return null;
		}

		private bool ItemInBranch(ItemData itemData, IEnumerable<ITaskRunner<TItem>> branch)
		{
			if (branch == null)
				return false;

			foreach (var runner in branch)
			{
				if (runner.TaskID == itemData.TaskID)
					return true;
				if (ItemInBranch(itemData, runner.Tasks))
					return true;
			}
			return false;
		}
	}


	class TaskExceptionHandler<TItem> : ITaskExceptionHandler<TItem>
	where TItem : WorkItem, new()
	{
		private ITaskRunner<TItem> target;

		public TaskExceptionHandler(ITaskRunner<TItem> target)
		{
			this.target = target;
		}

		public int TaskID { get => target.TaskID; }

		public async Task HandleException(ItemData itemData, Exception ex)
		{
			await target.MoveItemToTask(itemData);
		}
	}


	class FinallyExceptionHandler<TItem> : ITaskExceptionHandler<TItem>
		where TItem : WorkItem, new()
	{
		private ITaskRunner<TItem> target;

		public FinallyExceptionHandler(ITaskRunner<TItem> target)
		{
			this.target = target;
		}

		public int TaskID { get => target.TaskID; }

		public async Task HandleException(ItemData itemData, Exception ex)
		{
			itemData.RejectOnFinally = true;
			await target.MoveItemToTask(itemData);
		}
	}


	class FinallyAndCatchExceptionHandler<TItem> : ITaskExceptionHandler<TItem>
		where TItem : WorkItem, new()
	{
		private ITaskRunner<TItem> target;
		private int outstandingHandler;

		public FinallyAndCatchExceptionHandler(ItemDataModel model, ITaskRunner<TItem> target, int outstandingHandler)
		{
			this.target = target;
			this.outstandingHandler = outstandingHandler;
		}

		public int TaskID { get => target.TaskID; }

		public async Task HandleException(ItemData itemData, Exception ex)
		{
			itemData.OutstandingHandler = outstandingHandler;
			await target.MoveItemToTask(itemData);
		}
	}
}
