using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	public interface IItemLogEntry
	{
		long EntryID { get; }               // The id of the entry
		int WorkflowID { get; }             // The id of the workflow
		long ItemID { get; }                // The id of the Item
		int TaskID { get; }                 // The id of the task
		string TaskName { get; }            // The name of the task
		ItemLogEntryType EntryType { get; }	// The type of the entry: History, Message, Warning or Error
		string Message { get; }				// Message recorded in the log
		string ExceptionType { get; }       // In case EntryType is Error, contains the type of exception encountered
		string ExceptionMessage { get; }    // In case EntryTyoe is Error, contains the exception message (null if no exception was captured)
		string ExceptionStackTrace { get; } // In case EntryType is Error, contains the captured stack trace (null if no exception was captured)
		DateTime Date { get; }              // Date when this entry was created
	}

	public enum ItemLogEntryType
	{
		TaskHistory,    // The log entry was generated automatically as the item was executed in a given task
		Message,        // Messages logged directly from the workflow tasks the item has gone through
		Warning,        // Warning logged directly from the workflows tasks the item has gone through
		Error           // Error logged either by the tasks or by the workflow manager while trying to execute the item
	}

	public enum ItemLogEntrySubType
	{
		Success,     // HistoryEntry for a successful task execution
		Failure,     // HistoryEntry for a task interrupted by an exception
	}
}
