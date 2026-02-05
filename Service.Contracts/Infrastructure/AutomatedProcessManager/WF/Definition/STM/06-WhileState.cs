using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class WhileState<TItem>: TaskBlockState<TItem>
		where TItem: WorkItem
	{
		public WhileState(WFStateMachine<TItem> stm) : base(stm)
		{
		}

		public override void End()
		{
			if (stm.Node.ReverseSearch(node => node.ActionType.IsExecutableAction()).Count() == 0)
				throw new InvalidOperationException(Err(nameof(While)));

			stm.EndBlock();
		}
	}
}
