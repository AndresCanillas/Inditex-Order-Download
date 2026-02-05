using System;
using System.Collections.Generic;

namespace Service.Contracts.WF
{
	class WorkflowData
	{
		[PK, Identity]
		public int WorkflowID;          // ID of the workflow
		public string Name;             // Name of the workflow
		public bool Detached;			// Flag indicating if the workflow is detached (was once part of the system but has since been removed). Workflows can only be deleted from the database if they are detached.
	}

	class TaskData
	{
        [PK, Identity]
        public int TaskID;              // ID of the Task
		public int WorkflowID;          // ID of the workflow
		public string TaskName;         // Name of the task
		public bool CanRunOutOfFlow;    // Indicates if the task can run out of flow
		[Nullable]
		public TaskType TaskType;	    // The type of task
		public bool Detached;           // Flag indicating if the task is detached (was once part of the workflow but has since been removed). Tasks can only be deleted from the database if they are detached.
		public int SortOrder;			// Value used to sort tasks when querying the DB
	}

	enum TaskType
	{
		InsertTask = 1,
		ExecuteTask = 2,
		WaitTask = 3
	}


    // IMPORTANT: An item can be concurrently running in multiple workflows at the same time, and in each workflow the state of the item will be different. That is why the primary key is composed
    // by the WorkflowID + ItemID, to univocally indentify an item you need both IDs
	class ItemData
	{
		[PK]
		public int WorkflowID;              // ID of the workflow
		[PK]
		public long ItemID;                 // ID of the Item, its value is assigned from a sequence called "WFManagerSQ" and will never repeat for a given workflow.
		public int? ParentWorkflowID;       // An item can be assigned a parent (item), when that is done, we preserve the ID of the workflow and the ID of the original item in these fields.
        public long? ParentItemID;          // NOTE: The item parent can only be assigned when the item is newly created and has not been inserted in any workflow yet, once the item is inserted, the API will not allow you to change the Parent property.
        public string ItemName;             // The Name of the Item. NOTE: can only be set once, after being set this name cannot be changed through the API, this field can be used to ease searching for the item at a later time.
        public string Keywords;             // Keyworkds assigned to the item (used mainly to search items within the workflow). This field can be used to ease searching for the item at a later time.
		public int? TaskID;                 // The ID of the task the item is currently sitting at, this field will be null if the item is not currently executing in the workflow (ie. if it is complete or cancelled).
		public DateTime? TaskDate;          // Date of when the item got to the current task
		public ItemPriority ItemPriority;   // Priority of the item (higer priority items are executed before lower priority ones)
		public ItemStatus ItemStatus;       // The status of the item within the current task (Ready, Waiting, Delayed, Rejected)
		public string StatusReason;         // The last reason given that justifies the item status
		public string RouteCode;	        // The reoute code assigned by the last task that executed this item
		public DateTime DelayedUntil;	    // If item status is Delayed, then this field specifies until when the item should be retries again
		public int RetryCount;              // How many times the item has been tryed to execute in the current task
		public int MaxTries = 5;			// Maximum number of times this item should be attempted to execute before it is rejected.
		public WorkflowStatus WorkflowStatus;   // Status of the item within the whole workflow
		public int? CompletedFrom;          // When the workflow status is ForcedComplete or Cancelled, this will reference the task from which the item was foced out of the workflow. This field will be null if the item completed normally (by reaching the last task of the workflow).
		public DateTime? CompletedDate;     // Date of completion (or cancellation) within a given workflow.
        public string ItemState;            // Item state serialized as json 
        public string SourceEventState;     // Event state serialized as json that triggered the creation of this item (this will be null if the item is executing in the workflow as result of a direct workflow call)
		public string WakeEventState;       // State of the event that waked this item in the last wait task
		public bool RejectOnFinally;        // Flag indicating that this item is executing a finally block with an unhandled exception active. When set, the item will be rejected at the end of the active finally block.
		public int? OutstandingHandler;     // The ID of a task to which the item should be moved to after the current finally block completes.
		public DateTime? WakeTimeout;       // Date in which an item sitting on a waiting task that has a timeout should be awaken.
		[Nullable]
		public byte[] LastException;        // Stores the last exception catched by the workflow manager (NOTE: This field is used only when a .Try() block is defined within the workflow)
		public DateTime CreatedDate;		// When was this item initially created

        public ItemData()
        {
            ItemPriority = ItemPriority.Normal;
            ItemStatus = ItemStatus.Delayed;
            DelayedUntil = DateTime.Now.AddSeconds(-1);
        }
	}

	class ItemLog
	{
		[PK, Identity]
		public long EntryID;                     // Identity value used to ensure each entry has a unique key
		public int WorkflowID;                   // The id of the workflow
		public long ItemID;                      // The id of the Item
		public ItemLogEntryType EntryType;       // Type of log entry
		public ItemLogEntrySubType EntrySubType; // Sub type for the log entry
		public ItemLogVisibility Visibility;     // Indicates if the entry should be public or have restricted visibility
		public string AttachedData;              // An object serialized as json that can be used to store additional information. NOTE: For example TaskHistory entries include: ItemStatus, StatusReason, RouteCode, DelayTime, RetryCount, ExecutionTime
		public DateTime Date;                    // Creation Date of this entry
	}

	class BaseItemLogData
	{
		public int TaskID;						// Id of the task
		public string TaskName;					// Name of the task
		public string Message;                  // Message logged
		public string ExceptionType;            // In case the entry type is an error, contains the type of exception encountered
		public string ExceptionMessage;         // In case the entry type is an error, contains the exception message (null if no exception was captured)
		public string ExceptionStackTrace;      // In case the entry type is an error, contains the captured stack trace (null if no exception was captured)
	}

	class TaskHistoryLogData: BaseItemLogData
	{
        public bool Success;            // Flag indicating if the task was executed successfully or not
        public string Status;          	// The status returned by the task after executing the item
		public string StatusReason;     // Reason given (if any) as to why the status was set to the specified value (applies only for Rejected, Delayed, Completed & Cancelled statuses)
		public string RouteCode;		// Route code assigned to the item after the item executed in this task (applies only for Ok status)
		public int DelayTime;			// Delay given in seconds
		public int RetryCount;			// The value RetryCount had when this log entry was created
		public int ExecutionTime;		// given in milliseconds
	}


	public class CounterData
	{
		public int Active;
		public int Waiting;
		public int Delayed;
		public int Rejected;
		public int Completed;
		public int Cancelled;

		public CounterData() { }

		public CounterData(IEnumerable<ItemCounter> counters)
		{
			AddCounters(counters);
		}

		public void AddCounters(IEnumerable<ItemCounter> counters)
		{
			foreach (var c in counters)
			{
				switch (c.ItemStatus)
				{
					case ItemStatus.Active:
						Active += c.Counter;
						break;
					case ItemStatus.Waiting:
						Waiting += c.Counter;
						break;
					case ItemStatus.Delayed:
						Delayed += c.Counter;
						break;
					case ItemStatus.Rejected:
						Rejected += c.Counter;
						break;
					case ItemStatus.Completed:
						Completed += c.Counter;
						break;
					case ItemStatus.Cancelled:
						Cancelled += c.Counter;
						break;
				}
			}
		}
	}


	public class TaskCounterData
	{
		public int TaskID;
		public CounterData Counters = new CounterData();

		internal void AddCounter(TaskItemCounter counter)
		{
			switch (counter.ItemStatus)
			{
				case ItemStatus.Active:
					Counters.Active += counter.Counter;
					break;
				case ItemStatus.Delayed:
					Counters.Delayed += counter.Counter;
					break;
				case ItemStatus.Waiting:
					Counters.Waiting += counter.Counter;
					break;
				case ItemStatus.Rejected:
					Counters.Rejected += counter.Counter;
					break;
				case ItemStatus.Completed:
					Counters.Completed += counter.Counter;
					break;
				case ItemStatus.Cancelled:
					Counters.Cancelled += counter.Counter;
					break;
			}
		}
	}


	public class ItemCounter
	{
		public ItemStatus ItemStatus;
		public int Counter;
	}

	public class TaskItemCounter
	{
		public int TaskID;
		public ItemStatus ItemStatus;
		public int Counter;
	}

	public class StateCounterData
	{
		public string Value;
		public CounterData Counters = new CounterData();

		internal void AddCounter(StateItemCounter counter)
		{
			switch (counter.ItemStatus)
			{
				case ItemStatus.Active:
					Counters.Active += counter.Counter;
					break;
				case ItemStatus.Delayed:
					Counters.Delayed += counter.Counter;
					break;
				case ItemStatus.Waiting:
					Counters.Waiting += counter.Counter;
					break;
				case ItemStatus.Rejected:
					Counters.Rejected += counter.Counter;
					break;
				case ItemStatus.Completed:
					Counters.Completed += counter.Counter;
					break;
				case ItemStatus.Cancelled:
					Counters.Cancelled += counter.Counter;
					break;
			}
		}
	}

	public class StateItemCounter
	{
		public string Value;
		public ItemStatus ItemStatus;
		public int Counter;
	}


	public class WorkItemError
	{
		public int WorkflowID;
		public int TaskID;
		public long ItemID;
		public string ItemName;
		public ItemStatus ItemStatus;
		public string ItemState;
		public DateTime CreatedDate;
		public int ProjectID;
		public string LastErrorMessage;
		public DateTime LastErrorDate;
		public int LastErrorTaskID;
		public List<StateProperty> ExtraProperties { get; set; }
	}
}
