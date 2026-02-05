using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	public interface IWorkItem
	{
		/// <summary>
		/// The id of the workflow in which this item is executing
		/// </summary>
		int WorkflowID { get; }

		/// <summary>
		/// Unique identifier for the item, this is assigned internally by the APM when the item is created
		/// </summary>
		long ItemID { get; }

		/// <summary>
		/// Name of the item, should be set to something meaningful to ease finding items at a later time
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Keywords assigned to this item, can be used to narrow results during a search.
		/// </summary>
		string Keywords { get; set; }

		/// <summary>
		/// The ID of the task in which this item is currently queued. If null means the item is no longer actively executing in the workflow.
		/// </summary>
		int? TaskID { get; }

		/// <summary>
		/// The number of times this item has been attempted to execute in this task since it was first queued.
		/// </summary>
		/// <remarks>
		/// NOTES: This counter will be 0 the first time the item is executed. 
		/// This value will increase by one after each attempt of execution.
		/// This value will reset back to 0 once the item moves to a different task. 
		/// </remarks>
		int RetryCount { get; }

		/// <summary>
		/// Date since the item was moved to its current task
		/// </summary>
		DateTime? TaskDate { get; }

		/// <summary>
		/// Affects the order of execution of items in the workflow. All items default to Normal priority, and the priority can only be changed manually.
		/// NOTE: The priority will be reset back to Normal if there is any error while processing the item.
		/// </summary>
		ItemPriority ItemPriority { get; set; }

		/// <summary>
		/// The Status of the item within the current task
		/// </summary>
		ItemStatus ItemStatus { get; }

		/// <summary>
		/// The last reason given for an item to be put in its current state. You can see the item history for more information, and prior execution attempts.
		/// </summary>
		string StatusReason { get; }

		/// <summary>
		/// Date a which this item will be processed again, this date is meaningful only if the item status is Delayed. Otherwise this value should be ignored.
		/// </summary>
		DateTime DelayedUntil { get; }

		/// <summary>
		/// The route code assigned to the item. This value is reset to null when the item starts executing and needs to be set by the task code if the next
		/// task in the workflow is a Branch.
		/// </summary>
		string RouteCode { get; }

		/// <summary>
		/// A flag indicating if the item is active within the workflow or not. Active items are still somewhere within the workflow being processed or waiting for some event to happen.
		/// In the other hand if Active is false, it means that the item has gone through the whole workflow and is no longer executing or waiting on any task.
		/// You can reactivate an inactive item by Moving it to some task within the workflow.
		/// </summary>
		bool Active { get; }

		/// <summary>
		/// Value indicating how this item completed its processing within the workflow. Meaningful only if the item in no longer Active within the workflow.
		/// </summary>
		WorkflowStatus WorkflowStatus { get; }

		/// <summary>
		/// The task from which the item was completed (only meaningfull if the item is completed)
		/// </summary>
		int? CompletedFrom { get; }

		/// <summary>
		/// Date when the item was completed
		/// </summary>
		DateTime? CompletedDate { get; }

		/// <summary>
		/// Retrieves the saved state of the item (its user defined properties).
		/// </summary>
		/// <returns>Returns a json string containing the item saved state</returns>
		Task<string> GetSavedStateAsync();

		/// <summary>
		/// Retrieves the saved state of the item (its user defined properties).
		/// </summary>
		/// <returns>Returns a copy of the current item state</returns>
		Task<T> GetSavedStateAsync<T>() where T : WorkItem;

		/// <summary>
		/// Updates the saved state of the item (its user defined properties).
		/// </summary>
		/// <param name="state">A json string containing the item state</param>
		/// <remarks>
		/// Its easy to corrupt the state of the item if not careful, to avoid problems, the string passed as argument must be created from the data that was retrieved with GetSavedStateAsync method.
		/// IMPORTANT: This method will throw if the item is active in any given task, as updating the item state when it is active can lead to unexpected results.
		/// </remarks>
		Task UpdateSavedStateAsync(string state);

		/// <summary>
		/// Updates the saved state of the item (its user defined properties).
		/// </summary>
		/// <param name="state">An object representing the item state</param>
		/// <remarks>
		/// Its easy to corrupt the state of the item if not careful, to avoid problems, the object passed as argument must be the same object that was retrieved with GetSavedStateAsync method.
		/// IMPORTANT: This method will throw if the item is active in any given task, as updating the item state when it is active can lead to unexpected results.
		/// </remarks>
		Task UpdateSavedStateAsync<T>(T state) where T : WorkItem;

		/// <summary>
		/// Retrieves the history of the item, detailing all processes executed on the item so far (note: this is a subset of the item log).
		/// </summary>
		Task<IEnumerable<IItemHistoryEntry>> GetHistoryAsync();

		/// <summary>
		/// Retrieves the entire log of the item, detailing not only the task history but also, messages, warnings and errors.
		/// </summary>
		Task<IEnumerable<IItemLogEntry>> GetLogAsync();

		/// <summary>
		/// Retrieves the last exception catched by a try/catch block withing the workflow.
		/// </summary>
		Task<Exception> GetLastExceptionAsync();

		/// <summary>
		/// Preserves any changes made to the item properties.
		/// </summary>
		/// <remarks>
		/// This method will throw if the item has been updated since the moment this API object was snapshoted.
		/// </remarks>
		Task SaveAsync();
	}


	public enum ExecutionMode
	{
		InFlow = 1,     // Item is executin on this task normally
		OutOfFlow = 2   // Item is being forced to execute on this task out of flow, which means that the task should not attempt to change the item state.
	}


	public enum WorkflowStatus
	{
		InFlow = 1,         // Means that the item is still being processed within the workflow (The item is Active)
		Completed = 2,      // The item is no longer in the workflow (Item is inactive) because it was sucessfully completed
		ForcedCompleted = 3, // The item is no longer in the workflow (Item is inactive) and is considered as completed, but the item was not completed normally, instead it was forced to complete by calling the Complete method on the item.
		Cancelled = 4       // The item is no longer in the workflow (Item is inactive) because it was cancelled by calling the Cancel method on the item
	}


	/// <summary>
	/// Enumeration describing the statuses of items within a given task. Items in a task can be in any of these four states.
	/// </summary>
	public enum ItemStatus
	{
		Active = 1,         // Item is currently executing. While in this state, most API operations that try to change the item state or move the item to a different task will throw an error.
		Waiting = 2,        // Item is waiting for an undeterminated amount of time. This is the status for items whithin a WaitingTask, or when a subworkflow has been invoked. IMPORTANT: Moving a waiting item can cause the loss of the data received when the item is waked up, only move items in this state if you know that losing this information is aceptable.
		Delayed = 4,        // Item is waiting for a set amount of time before it is automatically retried (usually this is caused due to an error, altough the code can choose to delay an item for other reasons too).
		Rejected = 8,       // Item has been rejected and will not be retried any more without manual intervention from an administrator.
		Completed = 16,     // Item has been completed and should no longer be cosidered as part of the workflow.
		Cancelled = 32      // Item has been cancelled and should no longer be cosidered as part of the workflow.
	}


	/// <summary>
	/// An enumeration used to return results from workflow tasks. This value is used by the workflow manager to determine what to do with an item after a task finishes executing.
	/// </summary>
	public enum TaskStatus
	{
		OK = 1,         // Used to indicate that the item should flow to the next task in the workflow.
		Delayed = 2,    // Item will be delayed after the task completes execution
		Rejected = 3,   // Item will be rejected after the task completes execution
		Completed = 4,  // Item will be completed after the task completes execution
		Cancelled = 5,  // Item will be cancelled after the task completes execution
		Wait = 6,       // Item will be left waiting, this value is only accepted when returned from a waiting task
		Throw = 7,      // Task status that is valid only when returned from an .OnException handler, means that the exception handler wishes that the exception be handled by the workflow manager
		ReEnqueue = 8,  // Item will be returned to the end of the queue in the current task with minimal delay, can be used in long executing tasks to process by "chunks" and avoid blocking a task for large periods of time.
        SkipWait = 9    // Item will be moved to the next task
    }


	// Defines the different priorities for processing items
	// Items sharing the same priority are processed in the order they were received.
	// Higher priority items are processed before High, and High priority items are processed before Normal.
	public enum ItemPriority
	{
		Normal = 1,         // Default priority
		High = 2,           // Used to make an item take priority over normal items
		Highest = 3         // Used to make an item be processed with the highest priority
	}
}
