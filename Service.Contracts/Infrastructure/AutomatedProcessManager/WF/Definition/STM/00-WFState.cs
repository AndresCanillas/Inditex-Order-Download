using Rebex.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class WFState<TItem> 
		where TItem: WorkItem
	{
		protected WFStateMachine<TItem> stm;

		public WFState(WFStateMachine<TItem> stm)
		{
			this.stm = stm;
		}

		internal static string Err(string methodName)
		{
			return $"Calling {methodName} is not valid in the current state.";
		}

		public virtual void Insert(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Insert)));
		}

		public virtual void Execute(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Execute)));
		}

		public virtual void Action(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Action)));
		}

		public virtual void ExecuteWorkflow(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(ExecuteWorkflow)));
		}

		public virtual void Wait(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Wait)));
		}

		public virtual void SetTimeout(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(SetTimeout)));
		}

		public virtual void Route(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Route)));
		}

		public virtual void Branch(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Branch)));
		}

		public virtual void DefaultBranch(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(DefaultBranch)));
		}

		public virtual void MoveToBranch(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(MoveToBranch)));
		}

		public virtual void If(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(If)));
		}

		public virtual void ElseIf(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(ElseIf)));
		}

		public virtual void Else(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Else)));
		}

		public virtual void While(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(While)));
		}

		public virtual void Try(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Try)));
		}

		public virtual void Catch(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Catch)));
		}

		public virtual void Finally(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Finally)));
		}

		public virtual void OnException(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(OnException)));
		}

		public virtual void Raise(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(Raise)));
		}

		public virtual void MoveToTask(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(MoveToTask)));
		}

		public virtual void CancelItem(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(CancelItem)));
		}

		public virtual void CompleteItem(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(CompleteItem)));
		}

		public virtual void RejectItem(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(RejectItem)));
		}

		public virtual void DelayItem(TaskDescriptor<TItem> td)
		{
			throw new InvalidOperationException(Err(nameof(DelayItem)));
		}

		public virtual void End()
		{
			throw new InvalidOperationException(Err(nameof(End)));
		}

		public virtual void OnBlockEnding()
		{
		}
	}
}
