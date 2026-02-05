using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class IfState<TItem>: TaskBlockState<TItem>
		where TItem: WorkItem
	{
		public IfState(WFStateMachine<TItem> stm) : base(stm)
		{
		}

		public override void ElseIf(TaskDescriptor<TItem> td)
		{
			if (stm.Node.Parent.Children.Where(n => n.NodeType == WFNodeType.ElseBody).Count() > 0)
				throw new InvalidOperationException(Err(nameof(ElseIf)));
			stm.EndBlock();
			stm.StartBlock(td, stm.IfState);
		}

		public override void Else(TaskDescriptor<TItem> td)
		{
			if (stm.Node.Parent.Children.Where(n => n.NodeType == WFNodeType.ElseBody).Count() > 0)
				throw new InvalidOperationException(Err(nameof(Else)));
			stm.EndBlock();
			stm.StartBlock(td, stm.IfState);
		}

		public override void End()
		{
			stm.EndBlock();
			stm.EndBlock();
		}
	}
}
