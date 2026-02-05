using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	/// <summary>
	/// Base class used by all typed items.
	/// </summary>
	public abstract class WorkItem
	{
		private IFactory factory;
		private ItemData item;
		private bool detached;
		private ItemDataModel model;
		private string taskName;

		public WorkItem()
		{
			item = new ItemData() { TaskID = 0, TaskDate = DateTime.Now, CreatedDate = DateTime.Now };
			detached = true;
		}

        // IMPORTANT: do not try to initialize the item with this method as an end user of the APM, this is only meant for internal processes within the workflow manager.
		internal void InitItem(IFactory factory, ItemData item, string taskName)
		{
			this.factory = factory;
			this.item = item;
			this.taskName = taskName;
			this.model = factory.GetInstance<ItemDataModel>();
			detached = false;
		}

        /// <summary>
        /// The id of the workflow
        /// </summary>
        public int WorkflowID
        {
            get => item.WorkflowID;
            internal set => item.WorkflowID = value;
        }
		
		/// <summary>
		/// The id of the item. Item IDs are asigned by the workflow manager.
		/// </summary>
		public long ItemID
        {
            get => item.ItemID;
            internal set => item.ItemID = value;
        }

		/// <summary>
		/// The name of the item. This property can only be set once, after it is set trying to change its value will throw an exception. Item names are assigned by item creations tasks.
		/// </summary>
		public string ItemName
		{
			get => item.ItemName;
			set => item.ItemName = value;
		}

		public ItemPriority Priority
		{
			get => item.ItemPriority;
			set => item.ItemPriority = value;
		}
		
		/// <summary>
		/// Keywords assigned to the item. This value can be freely updated.
		/// </summary>
		public string Keywords
		{
			get => item.Keywords;
			set => item.Keywords = value;
		}

		/// <summary>
		/// The id of the current task
		/// </summary>
		public int TaskID
        {
            get => item.TaskID ?? 0;
            internal set => item.TaskID = value;
        }

		/// <summary>
		/// Allows to determine if the item is being executed normally (InFlow Execution Mode) or if it is running Out Of Flow. NOTE: Items cannot run in Out Of Flow mode unless the task is marked to allows this execution mode.
		/// </summary>
		public ExecutionMode ExecutionMode { get; internal set; }

		/// <summary>
		/// Indicates how many times this item has been retried, this is, how many times the Execute method has been invoked since the item entered this task.
		/// </summary>
		public int RetryCount { get => item.RetryCount; }

		/// <summary>
		/// Indicates how many times this item should be attempted to execute before being rejected in case of error, valid range is [1, N]
		/// </summary>
		public int MaxTries
		{
			get => item.MaxTries;
			set
			{
				if (value < 1)
					throw new InvalidOperationException($"Cannot set MaxRetries to {value}, valid range is: [1, N].");
				else
					item.MaxTries = value;
			}
		}

		/// <summary>
		/// Indicates since when the item has been in this task.
		/// </summary>
		public DateTime TaskDate { get => item.TaskDate??DateTime.Now; }

		public DateTime CreatedDate { get => item.CreatedDate; }

		/// <summary>
		/// This property will be true to indicate that the item was created outside the scope of a workflow task. Certain operations will not work when the item is detached.
		/// </summary>
        public bool Detached { get => detached; }

        /// <summary>
        /// Logs a message in the item log
        /// </summary>
        /// <param name="message">Message to be added to the item log as an informative message</param>
        /// <param name="visibility">Visibility of the entry</param>
        public async Task LogMessageAsync(string message, ItemLogVisibility visibility = ItemLogVisibility.Public)
		{
			if (detached)
				throw new InvalidOperationException("Item is not attached to a workflow yet.");
			await model.LogMessage(item, taskName, message, visibility);
		}

        /// <summary>
        /// Logs a warning in the item log
        /// </summary>
        /// <param name="message">Message to be added to the item log as a warning</param>
        /// <param name="visibility">Visibility of the entry</param>
        public async Task LogWarningAsync(string message, ItemLogVisibility visibility = ItemLogVisibility.Restricted)
		{
			if (detached)
				throw new InvalidOperationException("Item is not attached to a workflow yet.");
			await model.LogWarning(item, taskName, message, visibility);
		}

        /// <summary>
        /// Logs an exception in the item log
        /// </summary>
        /// <param name="message">Message to be added to the item log as an exception</param>
        /// <param name="ex">Catched exception</param>
        /// <param name="visibility">Visibility of the entry</param>
        public async Task LogExceptionAsync(string message, Exception ex, ItemLogVisibility visibility = ItemLogVisibility.Restricted)
        {
			if (detached)
				throw new InvalidOperationException("Item is not attached to a workflow yet.");
			await model.LogException(item, taskName, message, ex, visibility);
        }


		/// <summary>
		/// Logs an exception in the item log
		/// </summary>
		/// <param name="message">Message to be added to the item log as an exception</param>
		/// <param name="ex">Catched exception</param>
		/// <param name="visibility">Visibility of the entry</param>
		public async Task<Exception> GetLastExceptionAsync()
		{
			return await model.GetLastException(item);
		}

		public async Task SaveState()
		{
			item.ItemState = JsonConvert.SerializeObject(this);
            await model.SafeUpdate(item, new CancellationToken(),item.TaskID);
		}
	}

	public enum ItemLogVisibility
	{
		Public,		// Anyone can see public log entries
		Restricted  // Only selected user roles will see Restricted log entries. Note: The desicion to show or hide log messages is to be done by the application displaying the messages, not the Workflow Manager.
	}
}
