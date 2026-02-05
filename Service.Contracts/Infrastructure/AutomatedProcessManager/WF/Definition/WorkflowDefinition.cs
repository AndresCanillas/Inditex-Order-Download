using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	abstract class WorkflowDefinition
	{
		public WorkflowDefinition(string workflowName, Type itemType)
		{
			Name = workflowName;
			ItemType = itemType;
		}

		public int WorkflowID { get; internal set; }
		public string Name { get; }
		public Type ItemType { get; }

		public abstract void Validate(ICollection<WorkflowDefinition> allDefinedWorkflows);
	}


	class WorkflowDefinition<TItem> : WorkflowDefinition, IWorkflowDefinition<TItem> where TItem : WorkItem
	{
		private WFStateMachine<TItem> stm;

		public WorkflowDefinition(string workflowName)
			: base(workflowName, typeof(TItem))
		{
			stm = new WFStateMachine<TItem>();
		}

		public TaskNode<TItem> Root { get => stm.Root; }


		public override void Validate(ICollection<WorkflowDefinition> allDefinedWorkflows)
		{
			if (stm.State != stm.FinalState)
				throw new InvalidOperationException($"Workflow {Name} is not properly closed. You might be missing a call to .End()");
			var wfCalls = stm.Root.DeepSearch(n => n.ActionType == WFActionType.ExecuteWorkflowTask).ToList();
			if (allDefinedWorkflows == null)
				return;
			foreach (var node in wfCalls)
			{
				if (allDefinedWorkflows.Where(wf => String.Compare(wf.Name, node.Name, true) == 0).Count() == 0)
					throw new InvalidOperationException($"Call to ExecuteWorkflow(\"{node.Name}\") is invalid. The specified workflow has not been defined.");
			}
		}

		public IWorkflowDefinition<TItem> Insert<TEvent, TTask>()
			where TEvent : EQEventInfo
			where TTask : InsertTask<TEvent, TItem>
		{
			EnsureUniqueInputEvent(typeof(TEvent));
			Type tt = typeof(TTask);
			EnsureUniqueTaskName(tt);
			var td = new TaskDescriptor<TItem>()
			{
				Name = GetFriendlyName(tt),
				Description = GetTaskDescription(tt),
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.Executable,
				ActionType = WFActionType.InsertTask,
				TaskType = tt,
				EventType = typeof(TEvent),
				Options = tt.GetCustomAttribute<TaskOptionsAttribute>() ?? new TaskOptionsAttribute()
			};
			stm.State.Insert(td);
			return this;
		}

		public IWorkflowDefinition<TItem> Execute<TTask>() where TTask : WorkflowTask<TItem>
		{
			Type tt = typeof(TTask);
			EnsureUniqueTaskName(tt);
			var td = new TaskDescriptor<TItem>()
			{
				Name = GetFriendlyName(tt),
				Description = GetTaskDescription(tt),
				CanRunOutOfFlow = GetOutOfFlowFlag(tt),
				NodeType = WFNodeType.Executable,
				ActionType = WFActionType.ExecuteTask,
				TaskType = tt,
				Options = tt.GetCustomAttribute<TaskOptionsAttribute>() ?? new TaskOptionsAttribute()
		};
			stm.State.Execute(td);
			return this;
		}


		public IWorkflowDefinition<TItem> Action(Action<TItem> action)
		{
			var td = new TaskDescriptor<TItem>()
			{
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.RunAction,
				Callback = action 
			};
			stm.State.Action(td);
			return this;
		}


		public IWorkflowDefinition<TItem> ExecuteWorkflow(string workflowName)
		{
			if (String.IsNullOrWhiteSpace(workflowName))
				throw new InvalidOperationException("Invalid workflow name: The workflow name cannot be null or empty.");

			var td = new TaskDescriptor<TItem>()
			{
				Name = workflowName,
				Description = $"Runs items through the target workflow, execution in this workflow will resume after the target workflow completes.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.Executable,
				ActionType = WFActionType.ExecuteWorkflowTask,
				ParallelExecution = false,
			};
			stm.State.ExecuteWorkflow(td);
			return this;
		}

		public IWorkflowDefinition<TItem> ParallelExecuteWorkflow(string workflowName)
		{
			if (String.IsNullOrWhiteSpace(workflowName))
				throw new InvalidOperationException("Invalid workflow name: The workflow name cannot be null or empty.");

			var td = new TaskDescriptor<TItem>()
			{
				Name = workflowName,
				Description = $"Runs items through the target workflow, execution in this workflow will resume after the target workflow completes.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.Executable,
				ActionType = WFActionType.ExecuteWorkflowTask,
				ParallelExecution = true,
			};
			stm.State.ExecuteWorkflow(td);
			return this;
		}

		public IWorkflowDefinition<TItem> Wait<TEvent, TTask>(Expression<Func<TItem, TEvent, bool>> expression)
			where TEvent : EQEventInfo
			where TTask : WaitingTask<TEvent, TItem>
		{
			Type tt = typeof(TTask);
			EnsureUniqueTaskName(tt);
			var td = new TaskDescriptor<TItem>()
			{
				Name = GetFriendlyName(tt),
				Description = GetTaskDescription(tt),
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.Executable,
				ActionType = WFActionType.WaitTask,
				TaskType = tt,
				EventType = typeof(TEvent),
				WakeExpression = expression
			};
			stm.State.Wait(td);
			return this;
		}


		public IWorkflowDefinition<TItem> SetWaitTimeout(TimeSpan timeout)
		{
			
			var td = new TaskDescriptor<TItem>()
			{
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.None,
				ActionType = WFActionType.None,
				EventType = stm.Node.LastChild.EventType,
				WakeTimeout = timeout
			};
			stm.State.SetTimeout(td);
			return this;
		}


		public IWorkflowDefinition<TItem> Route()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = "Routing",
				Description = "Will evaluate the RouteCode generated by the previous task in order to decide which branch to execute next.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.RoutingScope,
				ActionType = WFActionType.None,
			};
			stm.State.Route(td);
			return this;
		}

		public IWorkflowDefinition<TItem> Branch(string routeCode)
		{
			if (String.IsNullOrWhiteSpace(routeCode))
				throw new InvalidOperationException("Invalid route code: The route code cannot be null or empty.");

			var td = new TaskDescriptor<TItem>()
			{
				Name = $"Branch \"{routeCode}\"",
				Description = $"Executes only if the previous task completed with the RouteCode \"{routeCode}\".",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.BranchBody,
				ActionType = WFActionType.None,
				RouteCode = routeCode
			};
			stm.State.Branch(td);
			return this;
		}

		public IWorkflowDefinition<TItem> DefaultBranch()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"Default Branch",
				Description = $"Executes only if the previous task completed with a RouteCode that does not match any of the other defined branches.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.BranchBody,
				ActionType = WFActionType.None,
				RouteCode = ""
			};
			stm.State.DefaultBranch(td);
			return this;
		}

		public IWorkflowDefinition<TItem> MoveToBranch(string routeCode)
		{
			if(String.IsNullOrWhiteSpace(routeCode))
				throw new InvalidOperationException("Invalid route code: The route code cannot be null or empty.");

			var td = new TaskDescriptor<TItem>()
			{
				Name = $"MoveToBranch(\"{routeCode}\")",
				Description = $"Items will move the the specified branch.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.MoveToBranch,
				RouteCode = routeCode
			};
			stm.State.MoveToBranch(td);
			return this;
		}

		public IWorkflowDefinition<TItem> MoveToDefaultBranch()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"MoveToDefaultBranch",
				Description = $"Items will move to the default branch.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.MoveToBranch,
				RouteCode = ""
			};
			stm.State.MoveToBranch(td);
			return this;
		}

		public IWorkflowDefinition<TItem> If(Expression<Predicate<TItem>> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			var td = new TaskDescriptor<TItem>()
			{
				Name = $"If({expression})",
				Description = $"Evaluates an expression to determine which task(s) to execute next.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.IfBody,
				ActionType = WFActionType.None,
				Expression = expression
			};
			stm.State.If(td);
			return this;
		}

		public IWorkflowDefinition<TItem> ElseIf(Expression<Predicate<TItem>> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			var td = new TaskDescriptor<TItem>()
			{
				Name = $"ElseIf({expression})",
				Description = $"Evaluates an expression to determine which task(s) to execute next.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.ElseIfBody,
				ActionType = WFActionType.None,
				Expression = expression
			};
			stm.State.ElseIf(td);
			return this;
		}

		public IWorkflowDefinition<TItem> Else()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"Else",
				Description = $"Executes only when all previous conditions are not meet.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.ElseBody,
				ActionType = WFActionType.None,
			};
			stm.State.Else(td);
			return this;
		}

		public IWorkflowDefinition<TItem> While(Expression<Predicate<TItem>> expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			var td = new TaskDescriptor<TItem>()
			{
				Name = $"While({expression})",
				Description = $"Evaluates an expression to determine if a block of task should be executed in a loop.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WhileBody,
				ActionType = WFActionType.None,
				Expression = expression
			};
			stm.State.While(td);
			return this;
		}

		public IWorkflowDefinition<TItem> Try()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"Try Body",
				Description = $"Wraps a set of tasks to provide customized error handling.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.TryBody,
				ActionType = WFActionType.None,
			};
			stm.State.Try(td);
			return this;
		}

		public IWorkflowDefinition<TItem> Catch()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"Catch",
				Description = $"Defines a set of tasks that will execute only if an error is encountered inside the preceding Try block.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.CatchBody,
				ActionType = WFActionType.None,
				ExceptionType = typeof(Exception)	// Catches all
			};
			stm.State.Catch(td);
			return this;
		}

		public IWorkflowDefinition<TItem> Catch<ExType>() where ExType : Exception
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"Catch",
				Description = $"Defines a set of tasks that will execute only if an error is encountered inside the preceding Try block.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.CatchBody,
				ActionType = WFActionType.None,
				ExceptionType = typeof(ExType)   // Catches only the specified type of exception
			};
			stm.State.Catch(td);
			return this;
		}

		public IWorkflowDefinition<TItem> Finally()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"Finally",
				Description = $"Defines a set of tasks that will execute regardless of what happens in the preceding Try/Catch blocks.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.FinallyBody,
				ActionType = WFActionType.None,
			};
			stm.State.Finally(td);
			return this;
		}

		public IWorkflowDefinition<TItem> OnException<ExType, THandler>()
			where ExType : Exception
			where THandler : TaskExceptionHandler<ExType, TItem>
		{
			var td = new TaskDescriptor<TItem>()
			{
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.None,
				ActionType = WFActionType.None,
				TaskType = typeof(THandler),
				ExceptionType = typeof(ExType)
			};
			stm.State.OnException(td);
			return this;
		}

		public IWorkflowDefinition<TItem> Raise<TEvent>() 
			where TEvent : WorkflowEventInfo<TItem>
		{
			var td = new TaskDescriptor<TItem>()
			{
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.RaiseEvent,
				EventType = typeof(TEvent)
			};
			stm.State.Raise(td);
			return this;
		}

		public IWorkflowDefinition<TItem> MoveTo<TTask>() where TTask : IWorkflowTask<TItem>
		{
			Type tt = typeof(TTask);
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"MoveTo {tt.Name}",
				Description = "Moves items to the specified task.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.MoveToTask,
				TaskType = tt
			};
			stm.State.MoveToTask(td);
			return this;
		}

		public IWorkflowDefinition<TItem> CancelItem()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"CancelItem",
				Description = "Cancels items (Removing them from the workflow with the Status Cancelled).",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.CancelItem,
			};
			stm.State.CancelItem(td);
			return this;
		}

		public IWorkflowDefinition<TItem> CompleteItem()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"CompleteItem",
				Description = "Completes items (Removing them from the workflow with the Status ForcedComplete).",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.CompleteItem,
			};
			stm.State.CompleteItem(td);
			return this;
		}


		public IWorkflowDefinition<TItem> RejectItem(string reason)
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"RejectItem",
				Description = "Moves items to the rejected state in the workflow.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.RejectItem,
				RejectReason = reason
			};
			stm.State.RejectItem(td);
			return this;
		}

		public IWorkflowDefinition<TItem> DelayItem()
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"DelayItem",
				Description = "Moves items to the delayed state in the workflow.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.DelayItem,
				DelayTime = null
			};
			stm.State.DelayItem(td);
			return this;
		}

		public IWorkflowDefinition<TItem> DelayItem(TimeSpan delaytime)
		{
			var td = new TaskDescriptor<TItem>()
			{
				Name = $"DelayItem",
				Description = "Moves items to the delayed state in the workflow.",
				CanRunOutOfFlow = false,
				NodeType = WFNodeType.WorkflowOp,
				ActionType = WFActionType.DelayItem,
				DelayTime = delaytime
			};
			stm.State.DelayItem(td);
			return this;
		}


		public IWorkflowDefinition<TItem> End()
		{
			stm.State.End();
			return this;
		}



		private Dictionary<Type, int> registeredEvents = new Dictionary<Type, int>();
		private Dictionary<Type, int> registeredTasks = new Dictionary<Type, int>();

		private void EnsureUniqueInputEvent(Type type)
		{
			if (!registeredEvents.TryGetValue(type, out _))
				registeredEvents.Add(type, 0);
			else
				throw new InvalidOperationException($"Source event {type.Name} is already registered. Cannot register the same input event multiple times within the same workflow. NOTE: If you need to run different processes that all start from the same event, then define multiple workflows.");
		}

		private void EnsureUniqueTaskName(Type type)
		{
			if (!registeredTasks.TryGetValue(type, out _))
				registeredTasks.Add(type, 0);
			else
				throw new InvalidOperationException($"Task {type.Name} is already registered. Cannot register the same task multiple times within the same workflow. NOTE: If you need to reuse a process in different places of the workflow, extract that funcionality to a service and reuse the service.");
		}

		private string GetFriendlyName(Type taskType)
		{
			var attrs = taskType.GetCustomAttributes(typeof(TaskInfoAttribute), false);
			if(attrs.Length > 0)
			{
				var taskInfo = attrs[0] as TaskInfoAttribute;
				return taskInfo.Name;
			}
			return taskType.Name;
		}

		private string GetTaskDescription(Type taskType)
		{
			var attrs = taskType.GetCustomAttributes(typeof(TaskInfoAttribute), false);
			if (attrs.Length > 0)
			{
				var taskInfo = attrs[0] as TaskInfoAttribute;
				return taskInfo.Description;
			}
			return "";
		}

		private bool GetOutOfFlowFlag(Type taskType)
		{
			var attrs = taskType.GetCustomAttributes(typeof(TaskInfoAttribute), false);
			if (attrs.Length > 0)
			{
				var taskInfo = attrs[0] as TaskInfoAttribute;
				return taskInfo.CanRunOutOfFlow;
			}
			return false;
		}
	}
}
