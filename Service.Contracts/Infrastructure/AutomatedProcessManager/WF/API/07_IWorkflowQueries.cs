using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	/// <summary>
	/// Provides methods to query information from the system workflows, their tasks and items.
	/// It is optimized for read-only operations and avoids much of the overhead incurred by the regular Workflow API (which is meant to make item state changes).
	/// </summary>
	public interface IWorkflowQueries
	{

		/// <summary>
		/// Gets summary information about all registered workflows and their tasks.
		/// </summary>
		Task<IEnumerable<WorkflowSummary>> GetWorkflows();

		/// <summary>
		/// Gets summary information of all tasks across all workflows.
		/// </summary>
		Task<IEnumerable<TaskSummary>> GetTasks();

		/// <summary>
		/// Gets item summary information, optionally applying several filters and pagination.
		/// </summary>
		Task<IEnumerable<ItemSummary>> GetItemSummary(PagedItemFilter filter);

		/// <summary>
		/// Gets item summary information, optionally applyng several filters and pagination.
		/// </summary>
		Task<IEnumerable<ItemSummary>> GetItemSummary<TItem>(
			PagedItemFilter filter,
			Expression<Func<TItem, bool>> expression) where TItem : WorkItem;

		/// <summary>
		/// Gets item counter information grouped by task, optionally applying severl filters.
		/// </summary>
		Task<Dictionary<int, TaskCounterData>> GetItemCountersGroupedByTaskAsync(ItemFilter filter);

		/// <summary>
		/// Gets item counter information grouped by task, optionally applying severl filters.
		/// </summary>
		Task<Dictionary<int, TaskCounterData>> GetItemCountersGroupedByTaskAsync<TItem>(
			ItemFilter filter,
			Expression<Func<TItem, bool>> expression) where TItem : WorkItem;

		/// <summary>
		/// Gets item counter information grouped by a sigle property from the item state (itemStatePropertyName), optionally filtering by workflowid,
		/// task ids, and specific values for the specified grouping property.
		/// </summary>
		Task<Dictionary<string, StateCounterData>> GetItemCountersGroupedByItemStateAsync<TValue>(
			ItemFilter filter,
			string itemStatePropertyName,
			IEnumerable<TValue> itemStateValues);

		/// <summary>
		/// Gets error information for items, optionally filtering by workflowid, tasks ids and item statuses
		/// </summary>
		Task<IEnumerable<WorkItemError>> GetTaskErrorsAsync(PagedItemFilter filter);

		/// <summary>
		/// Gets error information for items, optionally filtering by workflowid, tasks ids, item statuses and item state
		/// </summary>
		Task<IEnumerable<WorkItemError>> GetTaskErrorsByItemStateAsync<TItem>(
			PagedItemFilter filter,
			Expression<Func<TItem, bool>> expression) where TItem : WorkItem;

        /// <summary>
        /// Returns the last exception message for a set of items
        /// </summary>
        Task<IEnumerable<ItemExceptionInfo>> GetItemsLastExceptionsAsync(long worlflowId,IEnumerable<long> itemIds);
    }

	public class WorkflowSummary
	{
		public int WorkflowID { get; set; }
		public string Name { get; set; }
		public IEnumerable<TaskSummary> Tasks { get; set; }
	}

	public class TaskSummary
	{
		public int WorkflowID { get; set; }
		public int TaskID { get; set; }
		public string TaskName { get; set; }
	}

	public class ItemFilter
	{
		public int? WorkflowID { get; set; }
		public IEnumerable<int> Tasks { get; set; }
		public IEnumerable<ItemStatus> Statuses { get; set; }
		public int? ItemID { get; set; }
		public string Keywords { get; set; }
		public IEnumerable<string> IncludedStateProperties { get; set; }
	}

	public class PagedItemFilter : ItemFilter
	{
		public int? Page { get; set; }
		public int? PageSize { get; set; }
	}

	public class ItemSummary
	{
		public int WorkflowID { get; set; }
		public int TaskID { get; set; }
		public long ItemID { get; set; }
		public string ItemName { get; set; }
		public ItemStatus ItemStatus { get; set; }
		public DateTime CreatedDate { get; set; }
		public int ProjectID { get; set; }
		public List<StateProperty> ExtraProperties { get; set; }
	}

    public class  StateProperty
    {
		public string Name { get; set; }
		public string Value { get; set; }
	}

    public class ItemExceptionInfo
    {
        public long ItemId { get; set; }
        public string LastErrorMessage { get; set; }
        public DateTime? LasteErrorDate { get; set; }
        public int? LastErrorTaskId { get; set; }
    }

}
