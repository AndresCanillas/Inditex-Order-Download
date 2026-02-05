using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class InitialState<TItem> : WFState<TItem>
		where TItem : WorkItem
	{
		public InitialState(WFStateMachine<TItem> stm) : base(stm)
		{
		}

		public override void Insert(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td, stm.TaskBlockState);
		}

		public override void Execute(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td, stm.TaskBlockState);
		}

		public override void ExecuteWorkflow(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td, stm.TaskBlockState);
		}

		public override void If(TaskDescriptor<TItem> td)
		{
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

		public override void Action(TaskDescriptor<TItem> td)
		{
			stm.AddTask(td);
		}

		public override void End()
		{
			stm.EndBlock();
		}
	}
}
