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
	class ConditionalTaskRunner<TItem> : ITaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		class ConditionBranch
		{
			public TaskNode<TItem> Node;
			public List<ITaskRunner<TItem>> Tasks;
		}

		private IFactory factory;
		private ITaskRunner<TItem> parent;
		private TaskNode<TItem> taskNode;
		private Func<ItemData, Task> next;
		private List<ConditionBranch> branches;

		public ConditionalTaskRunner(IFactory factory)
		{
			this.factory = factory;
			branches = new List<ConditionBranch>();
		}

		public void Dispose()
		{
			// Need to pass the Dispose call along to all inner tasks
			foreach (var branch in branches)
			{
				foreach (var task in branch.Tasks)
					task.Dispose();
				branch.Tasks.Clear();
			}
			branches.Clear();
			branches = null;
			factory = null;
			taskNode = null;
			next = null;
		}

		public int WorkflowID { get => taskNode.WorkflowID; }

		public int TaskID { get => 0; }

		public string TaskName { get => "Conditional"; }

		public Type TaskType { get => null; }

		public ITaskRunner<TItem> Parent { get => parent; }

		public IEnumerable<ITaskRunner<TItem>> Tasks
		{
			get
			{
				foreach (var branch in branches)
				{
					foreach (var task in branch.Tasks)
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
				branches.Add(new ConditionBranch() { Node = node, Tasks = branchTasks });
			}
		}

		private async Task MoveToNextTask(ItemData item)
		{
			await next(item);
		}

		public async Task MoveItemToTask(ItemData itemData)
		{
			ConditionBranch matchingBranch = null;
			var itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
			itemInstance.InitItem(factory, itemData, TaskName);
			foreach (var branch in branches)
			{
				if (branch.Node.Expression == null)
				{
					matchingBranch = branch;
					break;
				}
				else if (branch.Node.Predicate(itemInstance))
				{
					matchingBranch = branch;
					break;
				}
			}

			// If no branch meets the condition, then move the item to the task that comes after the IF...
			if (matchingBranch == null)
			{
				await next(itemData);
			}
			else
			{
				// If the matched branch is not empty, move the item to its first task; otherwise move the item to the task that comes after the IF...
				if (matchingBranch.Tasks.Count > 0)
					await matchingBranch.Tasks[0].MoveItemToTask(itemData);
				else
					await next(itemData);
			}
		}


		// NOTE: This task does not really execute on its own, instead it moves items forward in the workflow by evaluating the predicates supplied in the workflow definition.
		public void Start(Func<ItemData, Task> next)
		{
			this.next = next;
		}

		public void Stop()
		{
			// Need to pass the stop signal along to all inner tasks
			foreach (var branch in branches)
			{
				foreach (var task in branch.Tasks)
					task.Stop();
			}
		}

		public async Task<bool> WaitForStop(TimeSpan timeout)
		{
			// Need to pass the wait along to all inner tasks
			bool result = true;
			foreach (var branch in branches)
			{
				foreach (var task in branch.Tasks)
					result &= await task.WaitForStop(timeout);
			}
			return result;
		}

		public Task<TaskResult> OutOfFlowExecute(ItemData item)
		{
			throw new Exception("Cannot execute a Conditional Task directly");
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
