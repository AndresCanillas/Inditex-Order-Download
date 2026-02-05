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
	class ExecuteTaskRunner<TItem> : MultiTaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		public ExecuteTaskRunner(IFactory factory)
			: base(factory)
		{

		}

		protected override async Task<TaskResult> ExecuteItemAsync(ItemData itemData, ExecutionMode executionMode, CancellationToken cancellationToken)
		{
			TItem itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
			itemInstance.ExecutionMode = executionMode;
			itemInstance.InitItem(factory, itemData, TaskName);
			var taskInstance = await AcquireTaskInstance();
			try
			{
				var result = await taskInstance.ExecuteAsync(itemInstance, cancellationToken);
				return result;
			}
			finally
			{
				ReleaseTaskInstance(taskInstance);
				itemData.ItemState = JsonConvert.SerializeObject(itemInstance);
			}
		}

		protected override async Task<TaskResult> ExecuteItemAsync(WorkflowTask<TItem> taskInstance, ItemData itemData, ExecutionMode executionMode, CancellationToken cancellationToken)
		{
			TItem itemInstance = JsonConvert.DeserializeObject<TItem>(itemData.ItemState);
			itemInstance.ExecutionMode = executionMode;
			itemInstance.InitItem(factory, itemData, TaskName);
			try
			{
				var result = await taskInstance.ExecuteAsync(itemInstance, cancellationToken);
				return result;
			}
			finally
			{
				itemData.ItemState = JsonConvert.SerializeObject(itemInstance);
			}
		}
	}
}
