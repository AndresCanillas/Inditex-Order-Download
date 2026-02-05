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
	class InsertTaskRunner<TEvent, TItem> : SingleTaskRunner<TItem>
		where TEvent : EQEventInfo
		where TItem : WorkItem, new()
	{
		protected InsertTask<TEvent, TItem> taskInstance;

		public InsertTaskRunner(IFactory factory)
			: base(factory)
		{
		}

		protected override TaskType BindTaskType { get => WF.TaskType.InsertTask; }


		public override void Start(Func<ItemData, Task> next)
		{
			taskInstance = factory.GetInstance(taskNode.TaskType) as InsertTask<TEvent, TItem>;
			events.OnEventRegistered += Events_OnEventRegistered;
			base.Start(next);
		}

		private void Events_OnEventRegistered(EQEventInfo e)
		{
			if (typeof(TEvent) == e.GetType())
			{
				Task.Factory.StartNew(async () =>
				{
					await itemModel.CreateItemForWorkflow(WorkflowID, new TItem(), TaskID, ItemStatus.Delayed, JsonConvert.SerializeObject(e));
					Wake();
				});
			}
		}


		// InsertTaskRunner does not accept items to be moved directly to them, instead they push the item forward.
		// This is because only items that originate from an event captured within the task itself can be in this task.
		public override async Task MoveItemToTask(ItemData item)
		{
			await next(item);   
		}


		protected override async Task<TaskResult> ExecuteItemAsync(ItemData itemData, ExecutionMode executionMode, CancellationToken cancellationToken)
		{
            if(string.IsNullOrWhiteSpace(itemData.SourceEventState))
                return TaskResult.OK();

			TItem itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
			itemInstance.ExecutionMode = executionMode;
			itemInstance.InitItem(factory, itemData, TaskName);
			TEvent eventInstance = JsonConvert.DeserializeObject<TEvent>(itemData.SourceEventState);
			try
			{
				var result = await taskInstance.Execute(eventInstance, itemInstance, cancellationToken);
				return result;
			}
			finally
			{
				itemData.ItemState = JsonConvert.SerializeObject(itemInstance);
			}
		}
	}
}
