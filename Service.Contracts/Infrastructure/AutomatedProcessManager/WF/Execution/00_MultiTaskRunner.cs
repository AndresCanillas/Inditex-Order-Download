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
using Service.Contracts.Authentication;
using Service.Contracts.Database;

namespace Service.Contracts.WF
{
	abstract class MultiTaskRunner<TItem> : BaseTaskRunner<TItem>
		where TItem : WorkItem, new()
	{
		private readonly int MAX_TASK_INSTANCES = 2;
		private ConcurrentQueue<WorkflowTask<TItem>> taskInstances;
		private AutoResetEvent runnerAvailable;

		public MultiTaskRunner(IFactory factory) : base(factory)
		{
			taskInstances = new ConcurrentQueue<WorkflowTask<TItem>>();
			runnerAvailable = new AutoResetEvent(false);
			var config = factory.GetInstance<IAppConfig>();
			var maxInstances = config.GetValue<int>("WF_MAX_TASK_INSTANCES");
			if (maxInstances > 0 && maxInstances <= 5)
				MAX_TASK_INSTANCES = maxInstances;
		}

		public override void Start(Func<ItemData, Task> next)
		{
			var maxTaskInstances = taskNode.Options?.MaxExecutionThreads ?? MAX_TASK_INSTANCES;
			for (int i = 0; i < maxTaskInstances; i++)
				taskInstances.Enqueue(factory.GetInstance(taskNode.TaskType) as WorkflowTask<TItem>);
			base.Start(next);
		}
		
		protected abstract Task<TaskResult> ExecuteItemAsync(WorkflowTask<TItem> taskInstance, ItemData itemData, ExecutionMode executionMode, CancellationToken cancellationToken);

		protected async Task<WorkflowTask<TItem>> AcquireTaskInstance()
		{
			WorkflowTask<TItem> taskInstance;
			while (!taskInstances.TryDequeue(out taskInstance))
				await runnerAvailable.WaitOneAsync(250, cts.Token);
			return taskInstance;
		}

		protected void ReleaseTaskInstance(WorkflowTask<TItem> taskInstance)
		{
			taskInstances.Enqueue(taskInstance);
			runnerAvailable.Set();
		}

		protected override async Task ExecuteItems()
		{
			while (readyItems.TryDequeue(out var item))
			{
				var taskInstance = await AcquireTaskInstance();
				_ = ExecuteItem(
						item,
						async () => await ExecuteItemAsync(taskInstance, item, ExecutionMode.InFlow, cts.Token)
					)
					.ContinueWith(async (t) =>
					{
						try
						{
							if (t.IsFaulted)
							{
								log.LogException($"WF ERROR - Exception while trying to execute item {item.ItemName}, item will be put back in the delayed state.", t.Exception);
								await itemModel.MakeDelayedInTask(item, "Unhandled Error in Workflow System", TimeSpan.FromMinutes(5), new SystemIdentity());
							}
						}
						catch { }
						finally
						{
							ReleaseTaskInstance(taskInstance);
						}
					});
			}
		}
	}
}