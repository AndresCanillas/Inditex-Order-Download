using Rebex.Net;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Service.Contracts.WF
{
	class TaskDescriptor<TItem>
		where TItem : WorkItem
	{
		private Expression<Predicate<TItem>> expression;
		private Predicate<TItem> predicate;

		public string Name;
		public string Description;
		public WFActionType ActionType;
		public WFNodeType NodeType;
		public bool ParallelExecution;
		public bool CanRunOutOfFlow;
		public string RouteCode;
		public Type TaskType;
		public Type ExceptionType;
		public Type EventType;
		public Expression WakeExpression;
		public TimeSpan WakeTimeout;
		public Action<TItem> Callback;
		public string RejectReason;
		public TimeSpan? DelayTime;
		public TaskOptionsAttribute Options;

		public Expression<Predicate<TItem>> Expression
		{
			get => expression;
			set
			{
				if (value == null)
					throw new InvalidOperationException("Cannot set Expression to null");
				expression = value;
				predicate = expression.Compile();
			}
		}

		public Predicate<TItem> Predicate { get => predicate; }
	}


	public enum WFActionType
	{
		None,
		InsertTask,
		ExecuteTask,
		WaitTask,
		ExecuteWorkflowTask,
		MoveToBranch,
		MoveToTask,
		CancelItem,
		CompleteItem,
		RejectItem,
		DelayItem,
		RaiseEvent,
		RunAction
	}

	public enum WFNodeType
	{
		None,
		WorkflowOp,
		RootBlock,
		Executable,
		RoutingScope,
		BranchBody,
		IfScope,
		IfBody,
		ElseIfBody,
		ElseBody,
		WhileBody,
		TryScope,
		TryBody,
		CatchBody,
		FinallyBody
	}

	static class WFActionTypeExtensions
	{
		public static bool IsExecutableAction(this WFActionType at)
		{
			return at == WFActionType.ExecuteTask || at == WFActionType.WaitTask || at == WFActionType.ExecuteWorkflowTask || at == WFActionType.InsertTask;
		}
	}
}
