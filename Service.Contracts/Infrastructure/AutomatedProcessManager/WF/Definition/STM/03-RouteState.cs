using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class RouteState<TItem>: WFState<TItem>
		where TItem : WorkItem
	{
		public RouteState(WFStateMachine<TItem> stm) : base(stm)
		{
		}

		public override void Branch(TaskDescriptor<TItem> td)
		{
			stm.Node.AddRoute(td.RouteCode);
			stm.StartBlock(td, stm.BranchState);
		}

		public override void DefaultBranch(TaskDescriptor<TItem> td)
		{
			stm.Node.AddRoute("");
			stm.StartBlock(td, stm.BranchState);
		}

		public override void End()
		{
			stm.EndBlock();
		}
	}
}
