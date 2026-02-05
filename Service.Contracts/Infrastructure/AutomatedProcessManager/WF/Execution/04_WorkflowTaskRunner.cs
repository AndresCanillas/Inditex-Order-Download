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
	class WorkflowTaskRunner<TItem> : SingleTaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private WFManager wfManager;
		private WFRunner<TItem> wfRunner;
		private int InvokedWorkflowID;

		public WorkflowTaskRunner(IFactory factory)
			: base(factory)
		{
		}


		public override void Start(Func<ItemData, Task> next)
		{
			wfManager = factory.GetInstance<WFManager>();
			wfRunner = wfManager.GetRunner<TItem>(taskNode.Name);
			InvokedWorkflowID = wfRunner.WorkflowID;
			events.OnEventRegistered += HandleEvent;
			base.Start(next);
		}


		private void HandleEvent(EQEventInfo e)
		{
			var evt = e as ItemCompletedEvent;
			if (evt != null && evt.WorkflowID == InvokedWorkflowID)
			{
				Task.Factory.StartNew(async () =>
				{
                    var itemData = await itemModel.GetItemByID(WorkflowID, evt.ItemID);
                    if(itemData.ItemStatus == ItemStatus.Waiting)
                    {
                        var elapsedMilliseconds = 0;
                        if(itemData.TaskDate.HasValue)
                            elapsedMilliseconds = (int)(DateTime.Now - itemData.TaskDate.Value).TotalMilliseconds;

                        if(evt.WorkflowStatus == WorkflowStatus.Cancelled)
                        {
                            await itemModel.AddItemHistoryEntry(TaskID, TaskName, itemData, TaskResult.Cancel("Item was cancelled in subworkflow"), elapsedMilliseconds);
                            await itemModel.CancelWaitingItem(WorkflowID, TaskID, evt.ItemID);
                        }
                        else
                        {
                            await itemModel.AddItemHistoryEntry(TaskID, TaskName, itemData, TaskResult.OK(), elapsedMilliseconds);
                            await next(itemData);
                        }
                    }
				});
			}
		}

		protected override async Task<TaskResult> ExecuteItemAsync(ItemData itemData, ExecutionMode executionMode, CancellationToken cancellationToken)
		{
            var itemDataSnapshot = JsonConvert.DeserializeObject<ItemData>(JsonConvert.SerializeObject(itemData));
			var itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
			itemInstance.InitItem(factory, itemDataSnapshot, TaskName);
			await wfRunner.InsertItem(itemInstance);
            if(!taskNode.ParallelExecution)
                itemData.ItemStatus = ItemStatus.Waiting;

            return TaskResult.OK();
        }
    }
}
