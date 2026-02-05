using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	public interface IWorkflow
	{
		/// <summary>
		/// Unique identifier for the workflow
		/// </summary>
		int WorkflowID { get; }

		/// <summary>
		/// Name of the workflow
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Returns a collection of all the task registered within the workflow
		/// </summary>
		IEnumerable<IWorkflowTask> GetTasks();

		/// <summary>
		/// Returns a task by its name
		/// </summary>
		/// <param name="name">Name of the task to retrieve</param>
		/// <returns>A matching task, or null if no match is found.</returns>
		IWorkflowTask GetTask(string name);

		/// <summary>
		/// Returns a task by its ID
		/// </summary>
		/// <param name="taskid">ID of the task to retrieve</param>
		/// <returns>A matching task, or null if no match is found.</returns>
		IWorkflowTask GetTask(int taskid);

		/// <summary>
		/// Gets the task with the specified task type T
		/// </summary>
		/// <typeparam name="T">The type of the task to retrieve.</typeparam>
		/// <remarks>
		/// This method will throw if the specified task type has not been registered within the workflow.
		/// </remarks>
		IWorkflowTask GetTask<T>();

		/// <summary>
		/// Gets a list of detached tasks.
		/// </summary>
		IEnumerable<IWorkflowTask> GetDetachedTasks();

		/// <summary>
		/// Removes a task that has become detached
		/// </summary>
		/// <param name="taskid">The id of the task to delete</param>
		/// <remarks>
		/// This method will throw if the specified task does not exist, if the task is not detached or if there are items still
		/// siting in the task that you are trying to delete.
		/// </remarks>
		Task DeleteDetachedTaskAsync(int taskid);

		/// <summary>
		/// Gets the number of items currently executing within the entire workflow, split by each possible state.
		/// </summary>
		Task<CounterData> GetItemCountersAsync();

		/// <summary>
		/// Gets the number of items currently executing within the entire workflow, split by each task.
		/// </summary>
		Task<Dictionary<int, TaskCounterData>> GetItemCountersByTaskAsync();

		/// <summary>
		/// Finds an item by its id, regardless of the task where it might be.
		/// </summary>
		/// <param name="itemid"></param>
		/// <returns>Returns the item found, or null if no match.</returns>
		/// <remarks>
		/// The returned items might move to a different task in the background, this is specially true for items in the Ready state. If you execute any operations on an item returned by
		/// the FindItem methods, be ready to handle exceptions releted to the item no longer being in the task you got them from.
		/// </remarks>
		Task<IWorkItem> FindItemAsync(long itemid);

		/// <summary>
		/// Finds items by their name, regardless of the task where they might be
		/// </summary>
		/// <param name="itemName">The name of the items to find.</param>
		/// <returns>Returns the item(s) that match the specified name, might return an empty collection if no matching items are found.</returns>
		/// <remarks>
		/// The returned items might move to a different task in the background, this is specially true for items in the Ready state. If you execute any operations on an item returned by
		/// the FindItem methods, be ready to handle exceptions releted to the item no longer being in the task you got them from.
		/// </remarks>
		Task<IWorkItem> FindItemAsync(string itemName);

		/// <summary>
		/// Finds items by their name and keywords, regardless of the task where they might be
		/// </summary>
		/// <param name="itemName">Name of the item</param>
		/// <param name="keywords">Any keywords that should be included in the search</param>
		/// <returns>Returns the item(s) that match the specified name or keywords, might return an empty collection if no matching items are found.</returns>
		/// <remarks>
		/// The returned items might move to a different task in the background, this is specially true for items in the Ready state. If you execute any operations on an item returned by
		/// the FindItem methods, be ready to handle exceptions releted to the item no longer being in the task you got them from.
		/// </remarks>
		Task<IEnumerable<IWorkItem>> FindItemsAsync(string itemName, string keywords, DateTime? fromdate, DateTime? todate);

		Task<IEnumerable<IWorkItem>> FindItemsAsync(int? taskid, ItemStatus? itemstatus, long? itemid, string itemName, string keywords, DateTime? fromdate, DateTime? todate);

		/// <summary>
		/// Inserts a new item into the workflow.
		/// </summary>
		/// <param name="item">The item to insert. An exception will be thrown if the specified item is not compatible with the item type of the workflow.</param>
		Task InsertItemAsync<TItem>(TItem item)
			where TItem : WorkItem, new();

        /// <summary>
        /// Inserts a new item into the workflow in the specified task and status.
        /// </summary>
        /// <param name="item">The item to insert. An exception will be thrown if the specified item is not compatible with the item type of the workflow or if the specified task does not belong to the workflow.</param>
        Task InsertItemAsync<TItem>(TItem item, int? taskid = null, ItemStatus? itemStatus = null, bool remoteWorkflow = false)
            where TItem : WorkItem, new();

        /// <summary>
        /// Delays an item to be executed later and lets you specify how long the item should be delayed.
        /// </summary>
        /// <param name="item">The item to be delayed</param>
        /// <param name="reason">The reason for the delay</param>
        /// <param name="delayTime">The amount of time that the item should be delayed</param>
        /// <remarks>
        /// The minimum allowed delay is one minute (if you specify a smaller delay, it will be overwriten)
        /// The maximum allowed delay is one day (if you specify a greater delay, it will be overwriten)
        /// 
        /// IMPORTANT: This method will thow an exception if the item is in the Active state, or if the item has been updated since
        /// the moment this API object was snapshoted.
        /// </remarks>
        Task DelayAsync(IWorkItem item, string reason, TimeSpan delayTime, IIdentity identity);

		/// <summary>
		/// This call will schedule the item to be executed as soon as possible.
		/// </summary>
		/// <remarks>
		/// IMPORTANT: This method will throw if the item is not in the Delayed or Rejected state or if the item has moved since the moment the 
		/// Item API object was snapshoted.
		/// </remarks>
		Task MakeActiveAsync(IWorkItem item, IIdentity identity = null);

		/// <summary>
		/// Puts the item in the rejected state. Items in the rejected state will no longer be retried and will sit in this task until
		/// an administrator decides what to do with the item.
		/// </summary>
		/// <param name="item">The item to be rejected</param>
		/// <param name="reason">The reason for putting this item in the rejected state.</param>
		/// <remarks>
		/// Items can be put in the rejected state by calling this method, aside from that, if an item is retried the maximum number of times,
		/// it will also be automatically put in the Rejected state.
		/// 
		/// IMPORTANT: This method will thow an exception if the item is not in the delayed or waiting state, or if the item has been updated since
		/// the moment this API object was snapshoted.
		/// </remarks>
		Task RejectAsync(IWorkItem item, string reason, IIdentity identity);

		/// <summary>
		/// Completes the item, moving it out of the worlflow and making it inactive.
		/// </summary>
		/// <param name="item">The item to be completed</param>
		/// <param name="reason">The reason why the item was completed.</param>
		/// <param name="identity">User requesting this action</param>
		/// <remarks>
		/// Same restrictions as with Move method, see Move method remarks for more information.
		/// </remarks>
		Task CompleteAsync(IWorkItem item, string reason, IIdentity identity);

		/// <summary>
		/// Cancels the item, moving it out of the worlflow and making it inactive.
		/// </summary>
		/// <param name="item">The item to be cancelled</param>
		/// <param name="reason">The reason why the item was cancelled.</param>
		/// <param name="identity">User requesting this action</param>
		/// <remarks>
		/// Same restrictions as with Move method, see Move method remarks for more information.
		/// </remarks>
		Task CancelAsync(IWorkItem item, string reason, IIdentity identity);

		/// <summary>
		/// Moves an item to the specified task, with the specified status. The given reason is stored in the item history along with the identity
		/// of the user that requested this change.
		/// </summary>
		/// <param name="item">The item to be moved</param>
		/// <param name="taskid">The id of the task the item should be moved to (can be the same task where the item is right now)</param>
		/// <param name="reason">The reason for moving this item to a different task / state.</param>
		/// <param name="status">The status the item once moved to the destination task.</param>
		/// <param name="identity">User requesting this action</param>
		/// <remarks>
		/// IMPORTANT: 
		///   Complete items can be moved any time without restrictions. 
		///   In the other hand, the following restrictions apply for active items:
		///   
		///     - This method will thow an exception if the item is no longer in the task it was when you got this item reference.
		///   
		///     - In order to be successfully moved, the current status of the item must be Delayed, Waiting or Rejected. Items in the Active state
		///       cannot be moved or changed in state, and an exception will be thrown if you try to move them.
		///     
		///	    - Take into account that the status of the item and its current task can quickly become stale, it can change between the moment you
		///	      get the item reference and the moment you call any of the methods that can change its state. These changes in state can, and will take 
		///	      place, without updating the state reflected by the API object. As such, you should treat the state shown by item references as a
		///	      snapshot of the state they had when the objects was created. You must be ready to handle exceptions related to the
		///	      item being in an invalid state, even if you think that you have made 'sure' that the operation you are trying to perform is valid.
		///	    
		///  Because of their volatile nature, objects representing work items should never be preserved in memory for extended periods of time, 
		///  it makes no sense for instance, to try to create a cache of items, or save item references "for later use". Instead you can preserve the
		///  item ID, and if you need to hydrate an instance, you can do so by calling workflow.FindItem, do what you have to do with the item,
		///  and then let the object instance fall out of scope to be garbage collected. API objects are light weight, and dont require any special
		///  logic to dispose them.
		/// </remarks>
		Task MoveAsync(IWorkItem item, int taskid, ItemStatus status, TimeSpan? delayTime, string reason, IIdentity identity);

		/// <summary>
		/// Ensures that the item is in a state that allows it to be moved to a different task without issues.
		/// </summary>
		Task<bool> CanMoveAsync(IWorkItem item);

		/// <summary>
		/// Updates the priority of the item to the specified value. NOTE: The priority of the item will be updated only if the item status is not active,
		/// if you try to update the priority of an item that is actively executing, an exception will be thrown.
		/// </summary>
		/// <param name="item">The item that will be updated</param>
		/// <param name="priority">The new priority of the item</param>
		/// <param name="identity">Identity of the user executing the operation</param>
		/// <returns></returns>
		Task ChangePriorityAsync(IWorkItem item, ItemPriority priority, IIdentity identity);

		/// <summary>
		/// Reactivates a cancelled item so it can resume execution from the point it was when cancelled/completed.
		/// </summary>
		/// <param name="item">The item to be reactivated</param>
		/// <param name="identity">User requesting this action</param>
		/// <remarks>
		/// Cancelling or Completing an item simply changes its Item Status, the rest of its state is not changed, this allows to
		/// reactivate the item right were it was before cancelling or completing it, and let the item continue processing.
		/// 
		/// Reactivate can only be called on cancelled or completed items. Optionally you can specify the id of the task where the 
		/// item should be moved to.
		/// 
		/// Notice however that this is not mandatory, meaning you can pass null. However, depending on the situation passing a null
		/// TaskID might not make sense, for instance if the item was completed normally, i.e., by running through the entire workflow,
		/// then the item will be reactivated by placing it in the last task of the workflow (which might not be desirable). At any
		/// rate, if you pass null as TaskID, then the item will be restored to the last task it was executing on, before it was
		/// completed/cancelled.
		/// </remarks>
		Task ReactivateAsync(IWorkItem item, int? taskid, ItemStatus status, TimeSpan? delayTime, string reason, IIdentity identity);

		/// <summary>
		/// Waits for the item to have the specified status before continuing
		/// </summary>
		/// <returns>Returns the item matching the specified ItemID</returns>
		Task<IWorkItem> WaitForItemStatus(long itemid, ItemStatus expectedItemStatus);
		Task<IWorkItem> WaitForItemStatus(long itemid, ItemStatus expectedItemStatus, TimeSpan timeout);

		/// <summary>
		/// Cancel all items in the workflow. Used mainly for test setup purposes.
		/// </summary>
		Task CancelAllItemsAsync(string reason);
	}
}
