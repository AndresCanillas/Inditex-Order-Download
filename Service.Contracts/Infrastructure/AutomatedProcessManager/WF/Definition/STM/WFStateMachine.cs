using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	class WFStateMachine<TItem> 
		where TItem: WorkItem
	{
		internal readonly WFState<TItem> InitialState;
		internal readonly WFState<TItem> TaskBlockState;
		internal readonly WFState<TItem> RouteState;
		internal readonly WFState<TItem> BranchState;
		internal readonly WFState<TItem> IfState;
		internal readonly WFState<TItem> WhileState;
		internal readonly WFState<TItem> TryState;
		internal readonly WFState<TItem> FinalState;

		private Stack<WFState<TItem>> stateStack = new Stack<WFState<TItem>>();
		private WFState<TItem> currentState;
		private TaskNode<TItem> root;
		private TaskNode<TItem> currentNode;

		internal WFState<TItem> State { get => currentState; }
		internal TaskNode<TItem> Root { get => root; }
		internal TaskNode<TItem> Node { get => currentNode; }

		public WFStateMachine()
		{
			InitialState = new InitialState<TItem>(this);
			TaskBlockState = new TaskBlockState<TItem>(this);
			RouteState = new RouteState<TItem>(this);
			BranchState = new BranchState<TItem>(this);
			IfState = new IfState<TItem>(this);
			WhileState = new WhileState<TItem>(this);
			TryState = new TryState<TItem>(this);
			FinalState = new WFState<TItem>(this);
			currentState = InitialState;
			root = currentNode = new TaskNode<TItem>();
		}

		internal void StartBlock(TaskDescriptor<TItem> td, WFState<TItem> newState)
		{
			if (td == null || newState == null)
				throw new InvalidOperationException();

			var n = new TaskNode<TItem>(currentNode, td);
			currentNode.Children.Add(n);
			currentNode = n;
			stateStack.Push(currentState);
			currentState = newState;
		}

		internal void EndBlock()
		{
			if (stateStack.Count == 0)
			{
				currentState = FinalState;
				ValidateWorkflowDefinition();
			}
			else
			{
				currentState.OnBlockEnding();
				currentNode = currentNode.Parent;
				currentState = stateStack.Pop();
			}
		}

		internal void AddTask(TaskDescriptor<TItem> td)
		{
			currentNode.Children.Add(new TaskNode<TItem>(currentNode, td));
		}

		internal void AddTask(TaskDescriptor<TItem> td, WFState<TItem> newState)
		{
			currentNode.Children.Add(new TaskNode<TItem>(currentNode, td));
			currentState = newState;
		}


		private void ValidateWorkflowDefinition()
		{
			ValidateRoutes(root);
			ValidateMoveToTask(root);
		}

		private void ValidateRoutes(TaskNode<TItem> node)
		{
			if (node.ActionType == WFActionType.MoveToBranch)
			{
				if (node.RouteCode == node.Parent.RouteCode)
					throw new Exception($"Cannot move to the same branch that is currently executing. RouteCode: {node.RouteCode}");

				if (!node.Parent.Parent.Routes.Contains(node.RouteCode))
				{
					var branchName = node.RouteCode != null ? node.RouteCode : "";
					throw new Exception($"Cannot move to branch \"{branchName}\", that branch has not been defined.");
				}
			}
			foreach (var child in node.Children)
				ValidateRoutes(child);
		}

		private void ValidateMoveToTask(TaskNode<TItem> node)
		{
			if (node.ActionType == WFActionType.MoveToTask)
			{
				var foundNodes = root.DeepSearch(n => (n.ActionType == WFActionType.ExecuteTask || n.ActionType == WFActionType.WaitTask) && n.TaskType == node.TaskType).Count();
				if (foundNodes != 1)
					throw new InvalidOperationException($"Cannot use MoveToTask<{node.TaskType.Name}>(). Task \"{node.TaskType.Name}\" cannot be found in this workflow or is registered more than once.");
			}
			foreach (var child in node.Children)
				ValidateMoveToTask(child);
		}
	}
}
