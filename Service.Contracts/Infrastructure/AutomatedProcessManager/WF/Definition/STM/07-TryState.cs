using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class TryState<TItem> : TaskBlockState<TItem>
		where TItem : WorkItem
	{
		public TryState(WFStateMachine<TItem> stm) : base(stm)
		{
		}

		public override void Catch(TaskDescriptor<TItem> td)
		{
			if (stm.Node.Parent.Children.Where(n => n.NodeType == WFNodeType.FinallyBody).Count() > 0)
				throw new InvalidOperationException(Err(nameof(Catch)));
			if (stm.Node.Parent.Children.Where(n => n.NodeType == WFNodeType.CatchBody && n.ExceptionType == td.ExceptionType).Count() > 0)
				throw new InvalidOperationException("Another catch for the same type of exception has already been defined in the current scope.");
			stm.EndBlock();
			stm.StartBlock(td, stm.TryState);
		}

		public override void Finally(TaskDescriptor<TItem> td)
		{
			if (stm.Node.Parent.Children.Where(n => n.NodeType == WFNodeType.FinallyBody).Count() > 0)
				throw new InvalidOperationException(Err(nameof(Finally)));
			stm.EndBlock();
			stm.StartBlock(td, stm.TryState);
		}

		public override void End()
		{
			stm.EndBlock();
			stm.EndBlock();
		}

		public override void OnBlockEnding()
		{
			if (stm.Node.NodeType == WFNodeType.TryBody || stm.Node.NodeType == WFNodeType.CatchBody || stm.Node.NodeType == WFNodeType.FinallyBody)
			{
				if (stm.Node.DeepSearch(n => n.NodeType == WFNodeType.Executable || n.NodeType == WFNodeType.WorkflowOp).Count() == 0)
					throw new InvalidOperationException($"{stm.Node.NodeType} must define at least one executable action.");
			}
			if (stm.Node.NodeType == WFNodeType.TryScope)
			{
				if (stm.Node.Children.Where(n => n.NodeType == WFNodeType.CatchBody || n.NodeType == WFNodeType.FinallyBody).Count() == 0)
					throw new InvalidOperationException($"Try block must define a Catch or a Finally block.");
			}
		}
	}
}
