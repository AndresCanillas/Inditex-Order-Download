using System;

namespace Services.Core
{
	public interface ILogEntry
	{
		int PID { get; }
		int TID { get; }
		DateTime Date { get; }
		LogEntryType EntryType { get; }
		string Section { get; }
		string Message { get; }
		string ExceptionType { get; }
		string StackTrace { get; }
		string UserName { get; }
		string RemoteHost { get; }
		string ToString(bool useShortVersion);
	}

	/// <summary>
	/// Enumerates the different event types that can be generated in the Application
	/// </summary>
	public enum LogEntryType
	{
		/// <summary>
		/// Just an informative message, usually contains tracing information
		/// </summary>
		Message = 1,
		/// <summary>
		/// A message used to report an anormal situation that should be reviewed but does not stop the application from working.
		/// </summary>
		Warning = 2,
		/// <summary>
		/// A message used to report a serious error that might be preventing the system from working properly.
		/// </summary>
		Exception = 4
	}
}
