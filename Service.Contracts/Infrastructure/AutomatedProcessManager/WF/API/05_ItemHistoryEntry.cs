using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	public interface IItemHistoryEntry : IItemLogEntry
	{
		bool Success { get; }
		string Status { get; }              // The status with which the item finished executing on the task
		string StatusReason { get; }        // Any reason given for the status (null if none was given)
		string RouteCode { get; }           // The route code the item got after running through the task (null if none was given)
		int DelayTime { get; }			    // If the status of the item was Delayed after runnig through the task, then this property will contain the amount of time (in minutes) the item was delayed by. Otherwise it will be Zero.
		int RetryCount { get; }             // The value of the item RetryCount when this history entry was created (null if no exception was captured)
		int ExecutionTime { get; }          // The amount of time in milliseconds it took the item to run through the task.
	}
}
