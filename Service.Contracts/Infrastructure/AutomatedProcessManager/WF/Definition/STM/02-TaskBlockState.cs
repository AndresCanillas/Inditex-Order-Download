using Rebex.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class TaskBlockState<TItem>: WFState<TItem>
		where TItem : WorkItem
	{
		public TaskBlockState(WFStateMachine<TItem> stm) : base(stm)
		{
		}

		public override void Insert(TaskDescriptor<TItem> td)
		{
			if (stm.Node.ReverseSearch(node => node.ActionType.IsExecutableAction() && node.ActionType != WFActionType.InsertTask).Count() > 0)
				throw new InvalidOperationException(Err(nameof(Insert)));
			stm.AddTask(td);
		}

		public override void Execute(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void Action(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void ExecuteWorkflow(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void Wait(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void SetTimeout(TaskDescriptor<TItem> td)
		{
			// SetTimeout is valid only after a Wait task
			if (stm.Node.LastChild == null || stm.Node.LastChild.ActionType != WFActionType.WaitTask)
				throw new InvalidOperationException(Err(nameof(SetTimeout)));

			stm.Node.LastChild.SetTimeout(td.WakeTimeout);
		}

		public override void Route(TaskDescriptor<TItem> td)
		{
			if (stm.Node.LastChild == null || !stm.Node.LastChild.ActionType.IsExecutableAction())
			{
				if (stm.Node.ReverseSearch(node => node.ActionType.IsExecutableAction()).Count() == 0)
					throw new InvalidOperationException(Err(nameof(Route)));
			}
			stm.StartBlock(td, stm.RouteState);
		}

		public override void If(TaskDescriptor<TItem> td)
		{
			if (stm.Node.LastChild == null || !stm.Node.LastChild.ActionType.IsExecutableAction())
			{
				if (stm.Node.ReverseSearch(node => node.ActionType.IsExecutableAction()).Count() == 0)
					throw new InvalidOperationException(Err(nameof(If)));
			}

			stm.StartBlock(new TaskDescriptor<TItem>() { Name = "If", NodeType = WFNodeType.IfScope }, stm.IfState);
			stm.StartBlock(td, stm.IfState);
		}

		public override void While(TaskDescriptor<TItem> td)
		{
			stm.StartBlock(td, stm.WhileState);
		}

		public override void Try(TaskDescriptor<TItem> td)
		{
			stm.StartBlock(new TaskDescriptor<TItem>() { Name = "Try", NodeType = WFNodeType.TryScope }, stm.TryState);
			stm.StartBlock(td, stm.TryState);
		}

		public override void OnException(TaskDescriptor<TItem> td)
		{
			// OnExeption is valid only after an executable task (Insert, Execute or Wait)
			if (stm.Node.LastChild == null || !stm.Node.LastChild.ActionType.IsExecutableAction())
				throw new InvalidOperationException(Err(nameof(OnException)));

			// OnException is NOT valid if used after ExecuteWorkflow or ParallelExecuteWorkflow
			if (stm.Node.LastChild.ActionType == WFActionType.ExecuteWorkflowTask)
				throw new InvalidOperationException(Err(nameof(OnException)));

			stm.Node.LastChild.AddExceptionHandler(td);
		}

		public override void Raise(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void MoveToTask(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void CancelItem(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void CompleteItem(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void RejectItem(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void DelayItem(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void End()
		{
			stm.EndBlock();
		}
	}
}
