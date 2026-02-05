using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Services.Core
{
	/// <summary>
	/// Represents information passed to a subscriber when the event AppEvents.OnRegisterEvent is raised.
	/// </summary>
	public class LogEntry : ILogEntry
	{
		private static int pid;


		static LogEntry()
		{
			pid = Process.GetCurrentProcess().Id;
		}

		private string message;
		private string exceptionType;
		private string stackTrace;

		/// <summary>
		/// The ID of the process
		/// </summary>
		public int PID { get; set; }

		/// <summary>
		/// The ID of the thread
		/// </summary>
		public int TID { get; set; }

		/// <summary>
		/// Date of the event as UTC
		/// </summary>
		public DateTime Date { get; set; }

		/// <summary>
		/// Describes the severity of the event.
		/// </summary>
		public LogEntryType EntryType { get; set; }

		public string Section { get; set; }

		/// <summary>
		/// Message describing where the exception was caught, and which operation was affected. It can also provide
		/// sugestions on how to correct or prevent the problem.
		/// </summary>
		public string Message
		{
			get { return message; }
			set
			{
				message = value;
				if(message != null && message.Length > 10000)
					message = message.Substring(0, 10000);
			}
		}

		/// <summary>
		/// The name of the System.Type of the exception, can be used to present a detailed (more technical)
		/// error report. Can be null if the EventType is Message or Warning.
		/// </summary>
		public string ExceptionType
		{
			get { return exceptionType; }
			set
			{
				exceptionType = value;
				if(exceptionType != null && exceptionType.Length > 100)
					exceptionType = exceptionType.Substring(0, 100);
			}
		}

		/// <summary>
		/// The stack trace of the exception, can be used to present a detailed (more technical) error report. Can be
		/// null if the EventType is Message or Warning.
		/// </summary>
		public string StackTrace
		{
			get { return stackTrace; }
			set
			{
				stackTrace = value;
				if(stackTrace != null && stackTrace.Length > 4000)
					stackTrace = stackTrace.Substring(0, 4000);
			}
		}

		/// <summary>
		/// User making the request (if any)
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// Remote host making the request (if any)
		/// </summary>
		public string RemoteHost { get; set; }

		//Default empty constructor, required for serialization
		public LogEntry()
		{
		}

		public LogEntry(string message)
			: this(message, null, LogEntryType.Message)
		{
		}

		public LogEntry(string message, LogEntryType eventType)
			: this(message, null, eventType)
		{
		}

		public LogEntry(Exception exception)
			: this("", exception, LogEntryType.Exception)
		{
		}

		public LogEntry(string message, Exception exception = null)
			: this(message, exception, LogEntryType.Exception)
		{
		}

		public LogEntry(string message, Exception ex, LogEntryType eventType, string userName = null, string remoteHost = null)
		{
			EntryType = eventType;
			Date = DateTime.Now.ToUniversalTime();
			PID = pid;
			TID = Thread.CurrentThread.ManagedThreadId;
			UserName = userName;
			RemoteHost = remoteHost;

			if(ex != null)
				Message = $"{message} {ex.Message}";
			else
				Message = message;

			if(ex != null)
			{
				ExceptionType = ex.GetType().Name;
				StringBuilder sb = new StringBuilder(1000);
				sb.AppendLine("------- Exception Caught ------");
				AppendException(sb, ex);
				StackTrace = sb.ToString();
			}
		}

		private void AppendException(StringBuilder sb, Exception ex, bool isInner = false)
		{
			sb.AppendLine("");

			if(isInner)
				sb.AppendLine("--- Inner Exception Details ---");

			sb.AppendLine("\tException Type: " + ex.GetType().Name);
			sb.AppendLine("\tException Message: " + ex.Message);
			sb.AppendLine($"\tStack Trace: {ex.StackTrace}");

			if(ex.InnerException != null)
				AppendException(sb, ex.InnerException, true);

			if(ex is AggregateException ae)
			{
				if(ae.InnerExceptions.Count > 0)
				{
					sb.AppendLine("\t----- [AggregateException Details START] -----");
					foreach(Exception aeInnerEx in ae.InnerExceptions)
					{
						AppendException(sb, aeInnerEx, true);
					}
					sb.AppendLine("\t----- [AggregateException Details END] -----");
				}
			}
		}


		public override string ToString()
		{
			return ToString(false);
		}


		public string ToString(bool useShortVersion)
		{
			StringBuilder sb = new StringBuilder(1000);
			if(useShortVersion)
			{
				if(EntryType == LogEntryType.Exception)
				{
					sb.AppendFormat("EXCEPTION: {0}\r\n", Message);
					sb.AppendLine("Exception Type: " + ExceptionType);
					sb.AppendLine(StackTrace);
				}
				else if(EntryType == LogEntryType.Warning)
				{
					sb.AppendFormat("WARNING: {0}\r\n", Message);
				}
				else
				{
					sb.AppendFormat("{0}\r\n", Message);
				}
			}
			else
			{
				if(EntryType == LogEntryType.Exception)
				{
					sb.AppendFormat("Date: {0} - EXCEPTION: {1}\r\n", Date.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss"), Message);
					sb.AppendLine($"\tException Type: {ExceptionType}");
					sb.AppendLine($"\t{StackTrace}");
				}
				else if(EntryType == LogEntryType.Warning)
				{
					sb.AppendFormat("Date: {0} - WARNING: {1}\r\n", Date.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss"), Message);
				}
				else
				{
					sb.AppendFormat("Date: {0} - MESSAGE: {1}\r\n", Date.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss"), Message);
				}
			}
			return sb.ToString();
		}
	}
}
