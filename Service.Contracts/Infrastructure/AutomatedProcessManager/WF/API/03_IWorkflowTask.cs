using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	public interface IWorkflowTask
	{
		/// <summary>
		/// The ID of the task
		/// </summary>
		int TaskID { get; }

		/// <summary>
		/// The name of the task
		/// </summary>
		string TaskName { get; }

		/// <summary>
		/// Indicates if this task allows to execute on items that are not currently queued in this task.
		/// </summary>
		bool CanRunOutOfFlow { get; }

		/// <summary>
		/// A flag indicating if this task is currectly taking part on the workflow processing. A task can become detached if it is removed from the workflow definition during initialization.
		/// NOTE: Detached task will not receive new items, and any items sitting in them will not be processed (even if they are in the Ready state).
		/// </summary>
		bool IsDetached { get; }

		/// <summary>
		/// Gets the number of items currently sitting within this task, split by each possible state.
		/// </summary>
		Task<CounterData> GetItemCountersAsync();

		/// <summary>
		/// Executes this task on the specified item without moving the item from its current location within the workflow.
		/// </summary>
		/// <param name="item">The item to be processed by this task</param>
		/// <remarks>
		/// This method will throw an exception if the task is not marked with the CanRunOutOfFlow flag.
		/// Executing a task in Out of Flow mode is a risky operation. A task marked with the CanRunOutOfFlow must be carefully designed to not create side
		/// effects or leave items and related database entities in an inconsistent state. This feature is provided mostly because we have the need of
		/// repeating certain operations within the workflow, without affecting where in the workflow an item might be. 
		/// 
		/// Prime examples of such operations include:
		/// - Generation of documents or reports
		/// - Sending of email notifications
		/// - Updating a print package and resending its notification to the local print system
		/// 
		/// IMPORTANT: 
		///   - ExecuteOutOfFlow can even invoked on an item that is no longer active within the workflow.
		///   - Out of Flow execution WILL NOT move the item from its current task or change its current state in any way. 
		///   - Even if the task updates the item properties, those changes will not be preserved and will be lost once the operation completes executing.
		///   - The workflow will not handle any exception generated as a result of executing the task in the Out of Flow mode. For instance, if the
		///     task sends email notifications and the email service is down, the item will not be placed in the delayed state and be retried later
		///     as it would normally be done when an item is executed within the workflow.
		///   - In case of an unhandled exception, the exception WILL propagate up the call stack to the caller, the caller is expected to handle these
		///     exceptions itself (if necessary).
		///   - The task will still generate a TaskResult, that result can be checked for an indication of the operation sucess or failure.
		/// </remarks>
		Task<TaskResult> ExecuteOutOfFlowAsync<TItem>(IWorkItem item)
			where TItem : WorkItem, new();
	}
}
