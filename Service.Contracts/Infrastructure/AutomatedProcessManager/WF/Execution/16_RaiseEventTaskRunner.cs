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
	class RaiseEventTaskRunner<TItem> : ITaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private readonly ItemDataModel itemModel;
		private readonly IFactory factory;
		private readonly IEventQueue events;

		private ITaskRunner<TItem> parent;
		private TaskNode<TItem> taskNode;
		private Func<ItemData, Task> next;

		public RaiseEventTaskRunner(
			ItemDataModel itemModel,
			IFactory factory,
			IEventQueue events)
		{
			this.itemModel = itemModel;
			this.factory = factory;
			this.events = events;
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
			var taskName = $"RaiseEvent<{taskNode.EventType.Name}>";

			TItem itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
			itemInstance.ExecutionMode = ExecutionMode.InFlow;
			itemInstance.InitItem(factory, itemData, taskName);

			var scope = factory.CreateScope() as IFactory;
			scope.RegisterSingleton<TItem>(itemInstance);
			var eventInstance = scope.GetInstance(taskNode.EventType);
			events.Send(eventInstance, EventSource.Local);

			await itemModel.LogMessage(itemData, taskName, $"{taskNode.EventType.Name} Raised!", ItemLogVisibility.Public);
			await next(itemData);
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
