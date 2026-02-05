using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class WorkflowTaskApi: IWorkflowTask
	{
		private IFactory factory;
		private ItemDataModel itemModel;
		private TaskData taskData;

		public int TaskID { get => taskData.TaskID; }
		public string TaskName { get => taskData.TaskName; }
		public bool CanRunOutOfFlow { get => taskData.CanRunOutOfFlow; }
		public bool IsDetached { get => taskData.Detached; }

		public WorkflowTaskApi(IFactory factory, ItemDataModel itemModel)
		{
			this.factory = factory;
			this.itemModel = itemModel;
		}

		internal void Initialize(TaskData taskData)
		{
			this.taskData = taskData;
		}

		public async Task<CounterData> GetItemCountersAsync()
		{
			return await itemModel.GetTaskCounters(taskData.WorkflowID, taskData.TaskID);
		}

		public async Task<TaskResult> ExecuteOutOfFlowAsync<TItem>(IWorkItem item)
			where TItem : WorkItem, new()
		{
			if (item == null || (item as WorkItemApi) == null)
				throw new InvalidOperationException("Argument 'item' is invalid.");

			var workflowManager = factory.GetInstance<WFManager>();
			var workflowRunner = workflowManager.GetRunner<TItem>(taskData.WorkflowID);
			if (workflowRunner == null)
				throw new InvalidOperationException($"Could not locate a runner for Workflow {taskData.WorkflowID}");
			var taskRunner = workflowRunner.GetTaskRunner(taskData.TaskID);
			if (taskRunner == null)
				throw new InvalidOperationException($"Could not locate a runner for Task {taskData.TaskID} in Workflow {taskData.WorkflowID}");

			var tr = await taskRunner.OutOfFlowExecute((item as WorkItemApi).itemData);
			return tr;
		}
	}
}
