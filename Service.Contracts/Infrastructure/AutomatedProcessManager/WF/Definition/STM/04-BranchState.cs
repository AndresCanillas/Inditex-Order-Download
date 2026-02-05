using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class BranchState<TItem>: TaskBlockState<TItem>
		where TItem: WorkItem
	{
		public BranchState(WFStateMachine<TItem> stm) : base(stm)
		{
		}

		public override void Branch(TaskDescriptor<TItem> td)
		{
			if(stm.Node.Children.Count > 0)
				stm.EndBlock();
			stm.Node.AddRoute(td.RouteCode);
			stm.StartBlock(td, stm.BranchState);
		}

		public override void DefaultBranch(TaskDescriptor<TItem> td)
		{
			if (stm.Node.Children.Count > 0)
				stm.EndBlock();
			stm.Node.AddRoute("");
			stm.StartBlock(td, stm.BranchState);
		}

		public override void MoveToBranch(TaskDescriptor<TItem> td)
		{
			if (stm.Node.RouteCode == td.RouteCode)
				throw new InvalidOperationException("Cannot move to the same branch that is currently executing.");
			stm.AddTask(td);
			stm.EndBlock();
		}

		public override void End()
		{
			stm.EndBlock();
			stm.EndBlock();
		}
	}
}
