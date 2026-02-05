using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	/// <summary>
	/// Represents a workflow task that handles item initialization right before items are inserted in the workflow.
	/// Item creation needs to be triggered by an event. Multiple item creation tasks can be registered in the workflow to respond to different types of events.
	/// </summary>
	/// <typeparam name="TItem">The item being initialized before it is inserted in the workflow.</typeparam>
	public abstract class InsertTask<TEvent, TItem>
		where TEvent : EQEventInfo
		where TItem : WorkItem
	{
		/// <summary>
		/// Initializes a new item that is about to be inserted in the workflow.
		/// </summary>
		/// <param name="e">The event that is triggering the item creation</param>
		/// <param name="item">The item that is being inserted in the workflow</param>
		/// <returns>This method must return a valid TaskResult which will be used by the workflow manager to determine what to do with the item after initialization is complete.</returns>
		/// <remarks>
		/// It is expected that item creation tasks set the item Name and Keywords fields as a minimum. In fact, the item Name cannot be changed after it has been set,
		/// and the workflow manager will Reject the item if its name is not set by the item creation task.
		/// 
		/// This type of task is also expected to initialize any other properties of the item that might be required by the next tasks that will execute in the workflow.
		/// 
		/// What the workflow manager does with the item after Execute returns depends on the type of result being returned from the Execute method.
		/// 
		/// Possible values include: 
		/// 
		///		- OK. The item is allowed to flow to the next task in the workflow. A route code can be supplied when returning an OK result, a route code is mandatory
		///		  if the next task in the workflow is a Routing task. If the next task is not a routing task, then the route code is ignored.
		///		  
		///		- Reject. The item is left in the Rejected state in the current task, until a user checks it and does something about it. When returning a reject result,
		///		  you must provide a non null, non empty reason as to why the item is being rejected. You can also include an exception object if the Reject is due to an
		///		  error, altough, in that case you could just not handle the exception and let the workflow manager reject the item for you.
		///		
		///		- Delay. This means that the item will be retried automatically again after the specified delay time elapses. When delaying an item you must also provide
		///		  a reason for why the item is being delayed. There is no limit to the amount of times an item can be delayed. Your code can evaluate things like the 
		///		  item RetryCount and the TaskDate to decide if the item should be delayed again or if maybe it is time to give up on this particular process.
		///		
		///		- Cancel. The item will be removed from the workflow entirely (no other task will execute on this item), the WorkflowResult of the item will be set to Cancelled.
		///		
		///		- Complete. The item will be removed from the workflow entirely (no other task will execute on this item), the WorkflowResult of the item will be set to ForcedComplete.
		///		
		///	Notes:
		///	
		///  > Returning null from this method will cause an error to be logged, and the item to be placed in the rejected state immediately.
		///    
		///  > In the case of creation tasks, not setting the item name before Execute returns, will cause the item to be rejected as well. Also, remember that the item
		///    name cannot be changed once it is set.
		///	
		///  > Unhandled exceptions occurring within the Execute method will automatically be logged and cause the item to be delayed by an increasing amount of time.
		///    The delay will start at one minute, and will increase by one minute with each consecutive failed attempt. If the item keeps throwing an exception after
		///    10 attempts, the item will be placed in the Rejected state, where it will remain until a user does something about the issue.
		///    
		///  > The workflow manager will send an APMErrorNotification event whenever an item is Rejected, this event can be used to send notifications through email,
		///    or add a record to some dashboard for instance.
		///  
		///  > Unless you have specific requirements to do so, it is not recomended for tasks to Cancel items directly (by returning a Cancel result), instead consider
		///    placing the item in the Rejected state, and let a user take the ultimate decision of cancelling the item. 
		///    
		///  > Force Completing an item directly from a task should also be carefully evaluated. It is just not frequent to have a good reason to Cancel or Complete
		///    an item directly from a task, as you might inadvertently cut the life of an item short too early, causing some processes to never have a chance to run.
		///    
		///  > If you have a business rule that would require an item to be cancelled (or completed), you can express that in the workflow definition instead of having
		///    the task cancel or complete the item directly. The advantage being that the workflow definition can be easily changed, but if you cancel or complete items
		///    in the task code, your workflow will be a bit less flexible and more opaque.
		///    
		///  > Don't be lazy when supplying a reason in your code, having a good description of the reason an item is being put in a particular state might be the
		///    difference between understanding what happenned to an item right away, or having to spend a lot of time trying to figure that out.
		/// </remarks>
		public abstract Task<TaskResult> Execute(TEvent e, TItem item, CancellationToken cancellationToken);
	}


	public interface IWorkflowTask<TItem>
		where TItem : WorkItem
	{

	}


	/// <summary>
	/// Represents a regular workflow task that processes items as soon as they arrive to the task.
	/// </summary>
	/// <typeparam name="TItem">The type of the item this task will process.</typeparam>
	public abstract class WorkflowTask<TItem> : IWorkflowTask<TItem>
		where TItem : WorkItem
	{
		/// <summary>
		/// Processes an item that has reached this task in the workflow.
		/// </summary>
		/// <param name="item">The item to be processed</param>
		/// <returns>This method must return a valid TaskResult which will be used by the workflow manager to determine what to do with the item after initialization is complete.</returns>
		/// <remarks>
		/// Same remarks as ItemCreationTask class
		/// </remarks>
		public abstract Task<TaskResult> ExecuteAsync(TItem item, CancellationToken cancellationToken);
	}


	/// <summary>
	/// Represents a set of operations that are executed in respose to errors in a task.
	/// </summary>
	/// <typeparam name="TItem">The type of the item this task will process.</typeparam>
	public abstract class TaskExceptionHandler<ExType, TItem>
		where ExType : Exception
		where TItem : WorkItem
	{
		/// <summary>
		/// Processes an exception generated while the item was being executed in the preceding task.
		/// </summary>
		/// <param name="item">The item that was being processed</param>
		/// <param name="ex">The exception that was captured</param>
		/// <param name="cancellationToken">A token used to request cancellation of this process</param>
		public abstract Task<TaskResult> ExecuteAsync(TItem item, ExType ex, CancellationToken cancellationToken);
	}


	public abstract class WorkflowEventInfo<TItem> : EQEventInfo
		where TItem: WorkItem
	{
		public TItem Item { get; set; }

		public WorkflowEventInfo(TItem item)
		{
			Item = item;
		}
	}


	/// <summary>
	/// Represents a task where items will be put in the waiting state until they are awaken by an specific event.
	/// </summary>
	/// <typeparam name="TEvent">The type of event this task will wait for before waking an item.</typeparam>
	/// <typeparam name="TItem">The type of the item this task will process.</typeparam>
	public abstract class WaitingTask<TEvent, TItem> : IWorkflowTask<TItem>
		where TEvent : EQEventInfo
		where TItem : WorkItem
	{
        /// <summary>
        /// This method is executed before the item is left in the waiting state.
        /// </summary>
        /// <param name="item">The item to process</param>
        /// <returns>A task result object indicating if the operation was successfull or not.</returns>
        /// <remarks>
        /// Derived classes must ensure to run any processes required to guarantee that the event that would wake the item is actually going to occur at some point in
        /// the future. What this processes are depends enterily on the operation of the workflow itself, and the inner workings of this task.
        /// 
        /// For instance, in the "Order Processing Workflow" we wait for the OrderValidatedEvent. So, in that case, the BeforeWaiting method should mark the order as
        /// "Pending for validation" so that the order shows up in the order report in a state that is consistent with the state of the associated Work Item within the
        /// workflow.
        /// 
        /// In this way, users will see that the order is pending validation in the orders report, and eventually someone can validate the order, triggering the 
        /// OrderValidatedEvent, which in turn will Wake the work item and allow the workflow to continue processing.
        /// 
        /// In this particular example, say that the status of the order was not "Pending Validation" before entering the wait task, and that we forget to update the
        /// status inside the BeforeWaiting method, then no one would be able to validate the order (as it is in an inconsistent state), and the work item would
        /// remain stuck in the waiting Task forever (or at least until a system administrator reviews any waiting items that might have been siting on this task
        /// for too long).
        /// 
        /// Returning a Wait/Ok/Delay result will cause the item to enter the waiting state. Other valid results are: Rejected, Delayed, Cancelled and Completed.
        /// As usual, it is not valid to return a task result with the item status Waiting. Route codes will be ignored.
        /// 
        /// NOTE: If no specific actions need to be run before the item is put in the waiting state, then you dont have to override this method.
        /// </remarks>
        public virtual Task<TaskResult> BeforeWaitingAsync(TItem item)
		{
			return Task.FromResult(TaskResult.Wait());
		}


		/// <summary>
		/// This method is executed when the event this task is waiting for awakens an item in the waiting state. This method can access any data
		/// received in the event to update the information of the item, or perform any required operations before letting the item continue its normal
		/// execution.
		/// </summary>
		/// <param name="e">The event that waked up this item</param>
		/// <param name="item">The item to be processed</param>
		/// <param name="cancellationToken">Cancellation token which signaled when the task is requested to stop</param>
		/// <returns>This method must return a valid TaskResult which will be used by the workflow manager to determine what to do with the item.</returns>
		/// <remarks>
		/// When the event that wakes up the item is received, the workflow manager will first execute the WakeItem method, after which, the item will
		/// inmediatelly be scheduled for execution, the regular Execute method will be called soon after that.
		/// 
		/// If the result returned by WakeItem is other than OK, then the item Execute method will not be run, and the item will be updated to match the 
		/// returned state. As usual, it is not valid to return a task result with the item status Waiting, also route codes will be ignored as routing
		/// can only happen after the Execute method has run.
		/// 
		/// It is also important to mention that items that are in the Waiting state can be manually forced to execute by a sysadmin (even if the event 
		/// that would wake them is not received). In that case, the user will be asked to provide the data of the event, and WakeItem will be
		/// invoked with the supplied information.
		/// 
		/// NOTE: If no specific actions needs to be run when the waking event is received, then you dont need to override this method, the item will execute
		/// even if you do not override WakeItem.
		/// </remarks>
		public abstract Task<TaskResult> ItemAwakeAsync(TEvent e, TItem item, CancellationToken cancellationToken);


		/// <summary>
		/// This method is executed when a waiting item times out, i.e., when the event that would wake the item is not recevied within the timeout period,
		/// the item is awoken and this method is run intead of ItemAwakeAsync. Notice that normally waiting tasks do not have a timeout, you need to invoke
		/// the SetTimeout method immediatelly after a WaitingTask in order to setup such a timeout.
		/// </summary>
		/// <param name="item">The item being timed out</param>
		/// <param name="cancellationToken">Cancellation token which signaled when the task is requested to stop</param>
		/// <returns>This method must return a valid TaskResult which will be used by the workflow manager to determine what to do with the item. Options include Reject, Ok, Complete, Cancel, Delay and Wait.</returns>
		public abstract Task<TaskResult> ItemTimeoutAsync(TItem item, CancellationToken cancellationToken);
	}
}
