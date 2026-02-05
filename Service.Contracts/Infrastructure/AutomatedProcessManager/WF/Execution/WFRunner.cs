using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Service.Contracts.Database;

namespace Service.Contracts.WF
{
	interface IWFRunner: IDisposable
	{
		int WorkflowID { get; }
		string Name { get; }
		Task Start(WorkflowDefinition wf);
		void Stop();
		Task WaitForStop();
	}

	class WFRunner<TItem> : IWFRunner
		where TItem : WorkItem, new()
	{
		private int workflowid;
		private string workflowName;
		private int maxItemRetryCount;

		private IFactory factory;
		private IEventQueue events;
		private WorkflowDataModel workflowModel;
		private ItemDataModel itemModel;
		private WorkflowDefinition<TItem> wf;
		private ITaskRunner<TItem> root;

		public int WorkflowID { get => workflowid; }
		public string Name { get => workflowName; }
		public string WorkflowName { get => workflowName; }
		public int MaxItemRetryCount { get => maxItemRetryCount; }

		public WFRunner(IFactory factory)
		{
			this.factory = factory;
			events = factory.GetInstance<IEventQueue>();
			workflowModel = factory.GetInstance<WorkflowDataModel>();
			itemModel = factory.GetInstance<ItemDataModel>();
		}

		public void Dispose()
		{
			root.Dispose();
		}

		public async Task Start(WorkflowDefinition wf)
		{
			if (wf == null)
				throw new ArgumentNullException(nameof(wf));

			var definition = wf as WorkflowDefinition<TItem>;
			if (definition == null)
				throw new InvalidOperationException("The given workflow definition is not complatible with this runner.");

			await InternalStart(definition);
		}

		private async Task InternalStart(WorkflowDefinition<TItem> wf)
		{
			this.wf = wf;
			workflowName = wf.Name;
			await workflowModel.EnsureDBCreated();
			var wfData = await workflowModel.BindWorkflowToDatabase(wf);
			wf.WorkflowID = wfData.WorkflowID;
			workflowid = wfData.WorkflowID;
			wf.Root.WorkflowID = workflowid;
			root = factory.GetInstance<RootTaskRunner<TItem>>();
			await root.Initialize(null, wf.Root);

			var wfManager = factory.GetInstance<WFManager>();
			var queue = wfManager.GetWorkflowQueue(workflowid);
			queue.Start();
		}

		public void Stop()
		{
			root.Stop();
		}

		public async Task WaitForStop()
		{
			await root.WaitForStop(TimeSpan.FromSeconds(10));
		}

		public async Task InsertItem(TItem item, int? taskid = null, ItemStatus? itemStatus = null)
		{
			var itemModel = factory.GetInstance<ItemDataModel>();
			var itemData = await itemModel.CreateItemForWorkflow(workflowid, item, taskid, itemStatus);
			await root.MoveItemToTask(itemData);
			item.InitItem(factory, itemData, root.TaskName);
		}

		public ITaskRunner<TItem> GetTaskRunner(int taskid)
		{
			return root.FindTaskRunner(taskid);
		}

		internal static async Task<List<ITaskRunner<TItem>>> CreateTasks(IFactory factory, ITaskRunner<TItem> parent, TaskNode<TItem> node, Func<ItemData, Task> cap)
		{
			var result = new List<ITaskRunner<TItem>>(100);
			ITaskRunner<TItem> task = null;
			foreach (var child in node.Children)
			{
				child.WorkflowID = node.WorkflowID;
				switch (child.NodeType)
				{
					case WFNodeType.Executable:
						switch (child.ActionType)
						{
							case WFActionType.InsertTask:
								task = factory.GetGenericInstance(typeof(InsertTaskRunner<,>), child.EventType, typeof(TItem)) as ITaskRunner<TItem>;
								break;
							case WFActionType.ExecuteTask:
								task = factory.GetInstance<ExecuteTaskRunner<TItem>>();
								break;
							case WFActionType.WaitTask:
								task = factory.GetGenericInstance(typeof(WaitTaskRunner<,>), child.EventType, typeof(TItem)) as ITaskRunner<TItem>;
								break;
							case WFActionType.ExecuteWorkflowTask:
								task = factory.GetInstance<WorkflowTaskRunner<TItem>>();
								break;
							default:
								throw new NotImplementedException($"Action {child.ActionType} not implemented");
						}
						break;
					case WFNodeType.WorkflowOp:
						switch (child.ActionType)
						{
							case WFActionType.MoveToBranch:
								task = factory.GetInstance<MoveToBranchTaskRunner<TItem>>();
								break;
							case WFActionType.MoveToTask:
								task = factory.GetInstance<MoveToTaskRunner<TItem>>();
								break;
							case WFActionType.CompleteItem:
								task = factory.GetInstance<CompleteItemTaskRunner<TItem>>();
								break;
							case WFActionType.CancelItem:
								task = factory.GetInstance<CancelItemTaskRunner<TItem>>();
								break;
							case WFActionType.RejectItem:
								task = factory.GetInstance<RejectItemTaskRunner<TItem>>();
								break;
							case WFActionType.DelayItem:
								task = factory.GetInstance<DelayItemTaskRunner<TItem>>();
								break;
							case WFActionType.RunAction:
								task = factory.GetInstance<ActionTaskRunner<TItem>>();
								break;
							case WFActionType.RaiseEvent:
								task = factory.GetInstance<RaiseEventTaskRunner<TItem>>();
								break;
							default:
								throw new NotImplementedException($"Action {child.ActionType} not implemented");
						}
						break;
					case WFNodeType.RoutingScope:
						task = factory.GetInstance<RoutingTaskRunner<TItem>>();
						break;
					case WFNodeType.IfScope:
						task = factory.GetInstance<ConditionalTaskRunner<TItem>>();
						break;
					case WFNodeType.WhileBody:
						task = factory.GetInstance<WhileTaskRunner<TItem>>();
						break;
					case WFNodeType.TryScope:
						task = factory.GetInstance<TryTaskRunner<TItem>>();
						break;
					default:
						throw new NotImplementedException($"NodeType {child.NodeType} not implemented");
				}
				await task.Initialize(parent, child);
				result.Add(task);
			}

			for (var i = result.Count - 1; i >= 0; i--)
			{
				var t = result[i];
				t.Start(cap);
				cap = t.MoveItemToTask;
			}
			return result;
		}
	}
}
