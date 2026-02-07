using System;

namespace Zebra.Sdk.Weblink
{
	/// <summary>
	///       Enumeration of a task's status.
	///       </summary>
	public enum ConfigurationStatus
	{
		/// <summary>
		///       Configuration state indicating the task has not been started.
		///       </summary>
		NOT_STARTED,
		/// <summary>
		///       Configuration state indicating the task is in process.
		///       </summary>
		IN_PROCESS,
		/// <summary>
		///       Configuration state indicating the task completed successfully.
		///       </summary>
		SUCCESSFULLY_COMPLETED,
		/// <summary>
		///       Configuration state indicating the task failed.
		///       </summary>
		CONFIGURATION_FAILED
	}
}