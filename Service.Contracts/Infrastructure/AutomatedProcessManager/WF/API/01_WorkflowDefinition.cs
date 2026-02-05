using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	public interface IWorkflowDefinition<TItem>
		where TItem : WorkItem
	{
		/// <summary>
		/// Registers an entry point for the workflow. The workflow manager will handle the specified event and invoke the registered Item Creator Task,
		/// and then insert the item in the workflow to start processing it.
		/// </summary>
		/// <typeparam name="TEvent">The type of the event the workflow will be listening to insert a new item.</typeparam>
		/// <typeparam name="THander">The item creator task that should handle the event and initialize the new item.</typeparam>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// A workflow can have multiple entry points, however it is not possible to register the same event more than once.
		/// Also, all entry points should be registered before any other tasks.
		/// </remarks>
		IWorkflowDefinition<TItem> Insert<TEvent, THandler>()
			where TEvent : EQEventInfo
			where THandler : InsertTask<TEvent, TItem>;

		/// <summary>
		/// Registers a new task in the workflow. Tasks execute in the order they are registered.
		/// </summary>
		/// <typeparam name="TTask">The WorkflowTask to register</typeparam>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// It is not possible to register the same task more than once in a workflow; if tasks share similar functionalities,
		/// then you should extract the shared code to a library or service, and create different task that use the library,
		/// instead of trying to register the same task in multiple places of the workflow.
		/// </remarks>
		IWorkflowDefinition<TItem> Execute<TTask>()
			where TTask : WorkflowTask<TItem>;

		/// <summary>
		/// Attaches an executable action to the workflow.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>Actions are meant to be one-liners (or no more than a few lines of codes long) to make changes on the properties of the item. IMPORTANT: Do not use Action to perform complex code/logic.</remarks>
		IWorkflowDefinition<TItem> Action(Action<TItem> action);

		/// <summary>
		/// Defines a special type of task that will have the item wait until the item finishes executing on the specified workflow.
		/// </summary>
		/// <param name="workflowName">The workflow that will be executed.</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> ExecuteWorkflow(string workflowName);

		/// <summary>
		/// Defines a special type of task that will execute the item in the specified workflow without waiting for the item to complete.
		/// Execution in the current workflow will run concurrently with the invoked workflow.
		/// </summary>
		/// <param name="workflowName">The workflow that will be executed.</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> ParallelExecuteWorkflow(string workflowName);

		/// <summary>
		/// Defines a wait task within the workflow.
		/// </summary>
		/// <typeparam name="TEvent">The type of event to wait for</typeparam>
		/// <typeparam name="TTask">The task that will handle the item and the event</typeparam>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// A Wait task is a special type of task that will put the items that enter it into the Waiting state.
		/// The items will remain in that state until an specific event is received. The specified task has two
		/// main responsabilities:
		/// 
		///		1) Ensure any processing required to guarrant the reception of the event is executed prior 
		///		   to putting the item in the waiting State.
		///		   
		///		2) Handling the event in question in order to wake the corresponding item and resume its
		///		   execution.
		/// </remarks>
		IWorkflowDefinition<TItem> Wait<TEvent, TTask>(Expression<Func<TItem, TEvent, bool>> expression)
			where TEvent : EQEventInfo
			where TTask : WaitingTask<TEvent, TItem>;

		/// <summary>
		/// Setups a timeout for the waiting item to wake up. If the item is not awaken before the specified timeout
		/// a default instance of the event will be raised by the workflow engine itself to wake the item automatically.
		/// Notice that setting a timeout is OPTIONAL, if not setup, items can stay in the task indefinitely.
		/// </summary>
		/// <param name="timeout"></param>
		/// <returns></returns>
		IWorkflowDefinition<TItem> SetWaitTimeout(TimeSpan timeout);

		/// <summary>
		/// Adds a new Routing Task in the workflow.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// Routing tasks are special workflow tasks, they will evaluate the RouteCode of an item 
		/// before the item leaves the current Task and enters any of the defined branches (branches
		/// are defined with the Branch and DefaultBranch methods).
		/// The workflow will allow the item to move forward only if it can find a branch with a
		/// matching route code, or if there is an default branch. Otherwise the Item will be put
		/// in the Rejected State and not be allowed to leave the current task.
		/// </remarks>
		IWorkflowDefinition<TItem> Route();

		/// <summary>
		/// Adds a branch with the specified route code to the current Routing task within the workflow.
		/// </summary>
		/// <param name="routeCode">The routeCode for the branch</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// Case will throw an error if you have not called the Route method to start a branch set
		/// definition. An exception will also be thrown if you define the same route code multiple times.
		/// NOTE: Route codes are NOT case sensitive.
		/// </remarks>
		IWorkflowDefinition<TItem> Branch(string routeCode);

		/// <summary>
		/// Defines a default branch within the current Routing task in the workflow.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// Else will throw an error if you have not called the Route method to start a branch set
		/// definition, or if you already called Else within the scope of the current Route task.
		/// </remarks>
		IWorkflowDefinition<TItem> DefaultBranch();

		/// <summary>
		/// Moves an item to the first task of the specified branch.
		/// </summary>
		/// <param name="routeCode">The routeCode of the branch that will be executed next.</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// MoveToBranch will throw an error if it is called outside the scope of a Route Task.
		/// An exception will also be thrown if the specified routeCode is not defined within the
		/// current branch set, or if the routeCode is the same as the currently executing branch.
		/// </remarks>
		IWorkflowDefinition<TItem> MoveToBranch(string routeCode);

		/// <summary>
		/// Moves an item to the first task of the default branch.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// MoveToDefaultBranch will throw an error if it is called outside the scope of a Route Task.
		/// An exception will also be thrown if a default branch has not been defined or if the method
		/// is called from within the default branch itself.
		/// </remarks>
		IWorkflowDefinition<TItem> MoveToDefaultBranch();

		/// <summary>
		/// Starts an If block on the current scope. Allows to evaluate a condition on the item before deciding which task to execute next.
		/// </summary>
		/// <param name="expression">A lamba expression that evaluates some condition on the item and returns true or false.</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> If(Expression<Predicate<TItem>> expression);

		/// <summary>
		/// Allows to create a sequence of multiple conditions. Take into account that when chaining conditions with ElseIf, only one of the
		/// defined branches will execute (the first whose predicate is evaluated to true). Once at least one branch executes, execution will
		/// flow to the first task defined after the End() matching the initial If() method.
		/// </summary>
		/// <param name="expression">A lambda expression that evaluates some condition on the item and returns true or false.</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> ElseIf(Expression<Predicate<TItem>> expression);

		/// <summary>
		/// Defines a "default" branch to be taken if none of the conditions are met.
		/// </summary>
		/// <param name="condition">A predicate that evaluates some condition on the item and returns true or false.</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> Else();

		/// <summary>
		/// Starts an While block on the current scope. Allows to evaluate a condition on the item to determine if the block of tasks should execute or not in a loop.
		/// </summary>
		/// <param name="expression">A lamba expression that evaluates some condition on the item and returns true or false.</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> While(Expression<Predicate<TItem>> expression);

		/// <summary>
		/// Starts a try/catch/finally block. Allows to define a set of tasks that will execute if an error is thrown from any of the task that are inside the "try" body.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> Try();

		/// <summary>
		/// Defines a catch-all branch that will execute regardless of the type of exception that might be encountered.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> Catch();

		/// <summary>
		/// Defines a catch branch that will execute only if the exception matches the given type.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> Catch<ExType>() where ExType : Exception;

		/// <summary>
		/// Defines a finally branch in the try block, that will execute regardless of having encountered an error or not.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> Finally();

		/// <summary>
		/// Defines an exception handler that is associated with the preceding executable task.
		/// </summary>
		/// <remarks>
		/// The OnException method can be called multiple times, all handlers will be associated with the preceding executable task.
		/// 
		/// The handlers themselves are NOT tasks, they will execute as if the code was embedded as part of the associated task,
		/// this means that when a handler registered with OnException is run, the item is still considered to be active within the
		/// associated task. 
		/// 
		/// A handler registered with OnException will run when the associated task throws an exception that matches with the handler's
		/// exception type. When a matching exception handler is run, it can read and make changes the state of the item, and it can
		/// return a TaskResult that will be used by the workflow manager to decide what to do with the item after the exception handler
		/// is run.
		///
		/// If no handler matches the exception being thrown, then no exception handler is run, and the item will continue it's normal
		/// exception handling flow.
		/// 
		/// You can use .OnException instead of Try/Catch/Finally when you only want to run a simple error handling logic, but do not wish
		/// to branch execution to a different location within the workflow.
		/// </remarks>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> OnException<ExType, THandler>()
			where ExType : Exception
			where THandler : TaskExceptionHandler<ExType, TItem>;	
		
		/// <summary>
		/// Raises the specified event as part of the workflow processing
		/// </summary>
		/// <typeparam name="TEvent">The type of the event to raise</typeparam>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// The specified event type must have a public constructor that takes a TItem as argument,
		/// otherwise Raise will throw an exception.
		/// </remarks>
		IWorkflowDefinition<TItem> Raise<TEvent>()
			where TEvent : WorkflowEventInfo<TItem>;

		/// <summary>
		/// Moves the item to the specified task as part of the workflow processing.
		/// </summary>
		/// <typeparam name="TTask">The type of the task where the item will be moved to.</typeparam>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// The item will be put in the target task with the Ready state. If the target task is not
		/// registered within the workflow before the AutomatedProcessManager is started, then an exception
		/// will be throw within the Start method.
		/// 
		/// This method is only valid as the last task within a given branch.
		/// </remarks>
		IWorkflowDefinition<TItem> MoveTo<TTask>()
			where TTask : IWorkflowTask<TItem>;

		/// <summary>
		/// Cancels all items that reach this task.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> CancelItem();

		/// <summary>
		/// Completes all items that reach this task.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// It is not necesary to invoke complete at the end of the workflow, items that reach the end of the workflow will become completed anyway.
		/// </remarks>
		IWorkflowDefinition<TItem> CompleteItem();

		/// <summary>
		/// Rejects all items that reach this task.
		/// </summary>
		/// <param name="reason">Reason why the item is being rejected, cannot be null or empty.</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> RejectItem(string reason);

		/// <summary>
		/// Delays all items that reach this task.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// The delay time will be automatically calculated by the workflow manager based of the RetryCount value, the more times an item has been retried, the longer the delay time will be (up to a max value of 60 minutes)
		/// </remarks>
		IWorkflowDefinition<TItem> DelayItem();

		/// <summary>
		/// Delays all items that reach this task.
		/// </summary>
		/// <param name="delaytime">The amount of time for the item to be delayed</param>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		/// <remarks>
		/// The items will always be delayed by the specified amount of time. Delaytime cannot be less than 30 seconds, or greather than 24hrs, the manager will clip the supplied time if necesary.
		/// </remarks>
		IWorkflowDefinition<TItem> DelayItem(TimeSpan delaytime);

		/// <summary>
		/// Used to mark the end of a multi-task container created by methods such as Route, If & Try.
		/// </summary>
		/// <returns>Returns the workflow object so you can keep chaining more tasks to it.</returns>
		IWorkflowDefinition<TItem> End();
	}
}
