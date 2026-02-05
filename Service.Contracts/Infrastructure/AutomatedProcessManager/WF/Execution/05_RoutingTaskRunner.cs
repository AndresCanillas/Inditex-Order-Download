using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class RoutingTaskRunner<TItem> : ITaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private IFactory factory;
		private ITaskRunner<TItem> parent;
		private TaskNode<TItem> taskNode;
		private Func<ItemData, Task> next;
		private Dictionary<string, List<ITaskRunner<TItem>>> index;
		private List<List<ITaskRunner<TItem>>> branches;

		public RoutingTaskRunner(IFactory factory)
		{
			this.factory = factory;
			index = new Dictionary<string, List<ITaskRunner<TItem>>>();
			branches = new List<List<ITaskRunner<TItem>>>();
		}

		public void Dispose()
		{
			// Need to pass the Dispose call along to all inner tasks
			foreach (var branch in branches)
			{
				foreach (var task in branch)
					task.Dispose();
			}
			branches.Clear();
			index.Clear();
			branches = null;
			index = null;
			factory = null;
			taskNode = null;
			next = null;
		}

		public int WorkflowID { get => taskNode.WorkflowID; }

		public int TaskID { get => 0; }

		public string TaskName { get => "Routing"; }

		public Type TaskType { get => null; }

		public ITaskRunner<TItem> Parent { get => parent; }

		public IEnumerable<ITaskRunner<TItem>> Tasks
		{
			get
			{
				foreach(var branch in branches)
				{
					foreach (var task in branch)
						yield return task;
				}
			}
		}

		public async Task Initialize(ITaskRunner<TItem> parent, TaskNode<TItem> taskNode)
		{
			this.parent = parent;
			this.taskNode = taskNode;
			foreach (var node in taskNode.Children)
			{
				node.WorkflowID = WorkflowID;
				var branchTasks = await WFRunner<TItem>.CreateTasks(factory, this, node, MoveToNextTask);
				index.Add(node.RouteCode, branchTasks);
				branches.Add(branchTasks);
			}
		}

		private async Task MoveToNextTask(ItemData item)
		{
			await next(item);
		}

		public async Task MoveItemToTask(ItemData item)
		{
			if (String.IsNullOrWhiteSpace(item.RouteCode))
				item.RouteCode = "";

			if (!index.TryGetValue(item.RouteCode, out var branch))
			{
				// If the supplied route code is not defined, check if there is a default
				// branch (the default branch will have am empty string as route code).
				index.TryGetValue("", out branch);
			}

			if (branch != null && branch.Count > 0)
			{
				// If we found a branch, move the item to that branch
				await branch[0].MoveItemToTask(item);
				return;
			}

			// If we reach this point, no matching branch was found, so move the item to the task that comes after the Routing task...
			await next(item);  
		}


		// NOTE: This task does not really execute anything...  It just moves items forward in the workflow based of the route code of the item...
		public void Start(Func<ItemData, Task> next)
		{
			this.next = next;
		}

		public void Stop()
		{
			// Need to pass the stop signal along to all inner tasks
			foreach(var branch in branches)
			{
				foreach (var task in branch)
					task.Stop();
			}
		}

		public async Task<bool> WaitForStop(TimeSpan timeout)
		{
			// Need to pass the wait along to all inner tasks
			bool result = true;
			foreach (var branch in branches)
			{
				foreach (var task in branch)
					result &= await task.WaitForStop(timeout);
			}
			return result;
		}

		public Task<TaskResult> OutOfFlowExecute(ItemData item)
		{
			throw new Exception("Cannot execute a BranchingTask directly");
		}

		public ITaskExceptionHandler<TItem> GetExceptionHandler(ItemData itemData, Exception ex, bool finallyLocked)
		{
			if (parent != null)
				return parent.GetExceptionHandler(itemData, ex, finallyLocked);
			else
				return null;
		}
	}
}
