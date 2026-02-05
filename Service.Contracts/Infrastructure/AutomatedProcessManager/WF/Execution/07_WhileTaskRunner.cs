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
	class WhileTaskRunner<TItem> : ITaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private IFactory factory;
		private ITaskRunner<TItem> parent;
		private TaskNode<TItem> taskNode;
		public List<ITaskRunner<TItem>> tasks;
		private Func<ItemData, Task> next;

		public WhileTaskRunner(IFactory factory)
		{
			this.factory = factory;
		}

		public void Dispose()
		{
			// Need to pass the Dispose call along to all inner tasks
			foreach (var task in tasks)
				task.Dispose();

			tasks.Clear();
			tasks = null;
			factory = null;
			taskNode = null;
			next = null;
		}

		public int WorkflowID { get => taskNode.WorkflowID; }

		public int TaskID { get => 0; }

		public string TaskName { get => "While"; }

		public Type TaskType { get => null; }

		public ITaskRunner<TItem> Parent { get => parent; }

		public IEnumerable<ITaskRunner<TItem>> Tasks
		{
			get
			{
				foreach (var task in tasks)
					yield return task;
			}
		}

		public async Task Initialize(ITaskRunner<TItem> parent, TaskNode<TItem> taskNode)
		{
			this.parent = parent;
			this.taskNode = taskNode;
			taskNode.WorkflowID = WorkflowID;
			if (taskNode.Predicate == null)
				throw new InvalidOperationException("While task runned required a predicate");
			tasks = await WFRunner<TItem>.CreateTasks(factory, this, taskNode, MoveToNextTask);
		}

		private async Task MoveToNextTask(ItemData item)
		{
			await MoveItemToTask(item);
		}

		public async Task MoveItemToTask(ItemData itemData)
		{
			var itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
			itemInstance.InitItem(factory, itemData, TaskName);

			// If expression is true, move to the first task in the while body, otherwise move the item to the task that comes after the While...
			if (taskNode.Predicate(itemInstance) == true)
			{
				await tasks[0].MoveItemToTask(itemData);
			}
			else
			{
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
			foreach (var task in tasks)
				task.Stop();
		}

		public async Task<bool> WaitForStop(TimeSpan timeout)
		{
			// Need to pass the wait along to all inner tasks
			bool result = true;
			foreach (var task in tasks)
				result &= await task.WaitForStop(timeout);
			return result;
		}

		public Task<TaskResult> OutOfFlowExecute(ItemData item)
		{
			throw new Exception("Cannot execute a While Task directly");
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
