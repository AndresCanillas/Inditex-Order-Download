using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Service.Contracts.Logging
{
    /// <summary>
    /// Provides a centralized place in which we can report any event or exception that occurs in any component
    /// of the system.
    /// </summary>
    /// <remarks>
    /// This class provides a mechanism for loging all messages, warnings and errors generated inside the Application.
    /// 
    /// It also provides an event to which any component in the system can subscribe to receive notifications of such
    /// errors (usually to display them to the user).
    ///			
    /// The log file is trimed automatically when its size grows greater than the maximum size (1MB by default).
    /// </remarks>
    class AppLog : IAppLog
	{
		private object syncObj = new object();
		private IFactory factory;
		private string baseDir;
		private string baseFileName;
		private string baseExtension;
		private int maxFileSize;
		private volatile IAppLogFile file;
		private List<EventHandler<ILogEntry>> subscribers = new List<EventHandler<ILogEntry>>();
		private ConcurrentQueue<ILogEntry> pendingEvents = new ConcurrentQueue<ILogEntry>();
		private ConcurrentDictionary<string, IAppLog> sections;
		private volatile bool terminated;
		private volatile LogLevel level;
		private volatile bool debugLogging;
		private bool logToConsole;
		[DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        public AppLog(IFactory factory)
		{
			this.factory = factory;

			ThreadPool.GetMinThreads(out var wt, out var iot);
			ThreadPool.SetMinThreads(wt*8, iot*8);

			lock (syncObj)
			{
				sections = new ConcurrentDictionary<string, IAppLog>();
				if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime) return;
                if (GetConsoleWindow() != IntPtr.Zero)
                {
                    logToConsole = true;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
				level = LogLevel.Default;
			}

			Task.Factory.StartNew(DoSendNotifications, TaskCreationOptions.LongRunning);
		}


		public void InitializeLogFile(string basePath, int maxFileSize = 4194304)
		{
			lock (syncObj)
			{
				if(file != null)
					file.Dispose();
				this.maxFileSize = maxFileSize;
				this.baseDir = Path.GetDirectoryName(basePath);
				if (String.IsNullOrWhiteSpace(baseDir))
				{
					var appInfo = factory.GetInstance<IAppInfo>();
					this.baseDir = appInfo.SystemLogDir;
				}
				this.baseFileName = Path.GetFileNameWithoutExtension(basePath);
				this.baseExtension = Path.GetExtension(basePath);
				file = factory.GetInstance<IAppLogFile>();
				var logfilename = Path.Combine(baseDir, baseFileName + baseExtension);
				file.Initialize(logfilename, maxFileSize);
            }
        }


		public IAppLog GetSection(string name)
		{
			IAppLog section;
			if (!sections.TryGetValue(name, out section))
			{
				lock (syncObj)
				{
					section = new AppLog(factory);
					lock (syncObj)
					{
						if (String.IsNullOrWhiteSpace(baseDir))
						{
							var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\"));
							InitializeLogFile(Path.Combine(assemblyDir, "logfile.log"), 4194304);
						}
					}
					string sectionFileName = FileStoreHelper.SanitizeFileName(name);
					string sectionFile = Path.Combine(baseDir, $"{baseFileName}.{sectionFileName}{baseExtension}");
					if (!sections.TryAdd(name, section))
						section = sections[name];
					else
						section.InitializeLogFile(sectionFile, maxFileSize);
				}
			}
			return section;
		}


		public LogLevel LogLevel
		{
			get { return level; }
			set { level = value; }
		}


		public bool DebugLogging
		{
			get => debugLogging;
			set => debugLogging = value;
		}


		/// <summary>
		/// This event is raised when an event is registered somewhere in the application.
		/// </summary>
		public event EventHandler<ILogEntry> OnEventRegistered
		{
			add { lock (subscribers) subscribers.Add(value); }
			remove { lock (subscribers) subscribers.Remove(value); }
		}


		public event EventHandler OnTerninate;


		public void LogMessage(string message, params object[] args)
		{
			if(args != null && args.Length > 0)
				LogEvent(LogLevel.Default, String.Format(message, args), null, LogEntryType.Message);
			else
				LogEvent(LogLevel.Default, message, null, LogEntryType.Message);
		}

		public void Trace(string message, params object[] args)
		{
			if (args != null && args.Length > 0)
				LogEvent(LogLevel.Verbose, String.Format(message, args), null, LogEntryType.Message);
			else
				LogEvent(LogLevel.Verbose, message, null, LogEntryType.Message);
		}

		public void LogMessage(LogLevel targetLevel, string message, params object[] args)
		{
			if (args != null && args.Length > 0)
				LogEvent(targetLevel, String.Format(message, args), null, LogEntryType.Message);
			else
				LogEvent(targetLevel, message, null, LogEntryType.Message);
		}

		public void LogWarning(string message, params object[] args)
		{
			if (args != null && args.Length > 0)
				LogEvent(LogLevel.Default, String.Format(message, args), null, LogEntryType.Warning);
			else
				LogEvent(LogLevel.Default, message, null, LogEntryType.Warning);
		}

		public void LogWarning(string message, Exception ex)
		{
			LogEvent(LogLevel.Default, message, ex, LogEntryType.Warning);
		}

		public void LogException(Exception ex)
		{
			if (ex != null)
				LogEvent(LogLevel.Critical, null, ex, LogEntryType.Exception);
		}

		public void LogException(string message, Exception ex)
		{
			if (ex != null)
				LogEvent(LogLevel.Critical, message, ex, LogEntryType.Exception);
		}

		public void LogEvent(LogLevel targetLevel, string message, Exception exception, LogEntryType eventType)
		{
			if (((int)level) >= ((int)targetLevel))
			{
				AppLogEntry e = new AppLogEntry(message, exception, eventType);
				LogEvent(e);
			}
		}

		public void LogEvent(ILogEntry e)
		{
			if (terminated) return;

			pendingEvents.Enqueue(e);

			while (pendingEvents.Count > 500)
				Thread.Sleep(50);
		}


		private void DoSendNotifications()
		{
			bool keepWorking;
			ILogEntry e;
			do
			{
				try
				{
					do
					{
						keepWorking = false;
						e = null;
						if (pendingEvents.Count > 0)
							keepWorking = pendingEvents.TryDequeue(out e);

						if (keepWorking)
						{
							// Notifies all subscribers, if a subscriber raises an exception the notification process will not get interrupted (all other subscribers will receive the event).
							lock (subscribers)
							{
								foreach (EventHandler<ILogEntry> s in subscribers)
								{
									if (s != null)
									{
										try
										{
											s(null, e);
										}
										catch { }
									}
								}
							}

							if (logToConsole)
								Console.Write(e.ToString());

							if (debugLogging)
								Debug.Write(e.ToString());

							if (file != null)
							{
								try
								{
									file.LogEvent(e);
								}
								catch (Exception ex)
								{
									if (logToConsole)
									{
										var evt = new AppLogEntry("", ex, LogEntryType.Exception);
										Console.Write(evt.ToString());
									}
								}
							}
						}
					} while (keepWorking);
				}
				catch (Exception ex)
				{
					if (logToConsole)
						Console.Write(ex.Message);

					if (debugLogging)
						Debug.Write(ex.Message);
				}
				finally
				{
					Thread.Sleep(50);
				}
			} while (!terminated);
		}



		public void WaitForPendingNotifications()
		{
			bool swWait;
			do
			{
				lock (pendingEvents)
					swWait = pendingEvents.Count > 0;
				if (swWait) Thread.Sleep(50);
			} while (swWait);
		}


		public void Terminate()
		{
			foreach (var section in sections.Values)
				section.Terminate();
			WaitForPendingNotifications();
			terminated = true;
			OnTerninate?.Invoke(null, EventArgs.Empty);
			lock (subscribers)
				subscribers.Clear();
			OnTerninate = null;
		}
	}


	/// <summary>
	/// Represents information passed to a subscriber when the event AppEvents.OnRegisterEvent is raised.
	/// </summary>
	public class AppLogEntry: ILogEntry
	{
		private static int pid;


		static AppLogEntry()
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
				if (message != null && message.Length > 10000)
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
				if (exceptionType != null && exceptionType.Length > 100)
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
			}
		}

		//Default empty constructor, required for serialization
		public AppLogEntry()
		{
		}

		public AppLogEntry(string message)
			: this(message, null, LogEntryType.Message)
		{
		}

		public AppLogEntry(string message, LogEntryType eventType)
			: this(message, null, eventType)
		{
		}

		public AppLogEntry(Exception exception)
			: this("", exception, LogEntryType.Exception)
		{
		}

		public AppLogEntry(string message, Exception exception)
			: this(message, exception, LogEntryType.Exception)
		{
		}

		internal AppLogEntry(string message, Exception ex, LogEntryType eventType)
		{
			EntryType = eventType;
			Date = DateTime.Now.ToUniversalTime();
			PID = pid;
			TID = Thread.CurrentThread.ManagedThreadId;

			if (ex != null)
				Message = $"{message} {ex.Message}";
			else
				Message = message;

			if (ex != null)
			{
				if (ex is MsgException)
				{
					var msgx = ex as MsgException;
					Message = $"{message} {msgx.Message} << remote boundary >> {msgx.OriginalMessage}";
					ExceptionType = $"MsgException << remote boundary >> {msgx.OriginalType}";
				}
				else
				{
					ExceptionType = ex.GetType().Name;
				}

				StringBuilder sb = new StringBuilder(1000);
				sb.AppendLine("------- Exception Caught ------");
				AppendException(sb, ex);
				StackTrace = sb.ToString();
			}
		}

		private void AppendException(StringBuilder sb, Exception ex, bool isInner = false)
		{
			sb.AppendLine("");

			if (isInner)
				sb.AppendLine("--- Inner Exception Details ---");

			sb.AppendLine("\tException Type: " + ex.GetType().Name);
			sb.AppendLine("\tException Message: " + ex.Message);
			try { sb.AppendLine("\tSource: " + ex.Source); }  // Need try/catch cause some times reading Source throws an error :S
			catch { }

			if (ex is MsgException)
			{
				var msgx = ex as MsgException;
				sb.Append($"\tOriginal Exception Message: {msgx.OriginalMessage}\r\n");
				sb.Append($"\tOriginal Exception Type: {msgx.OriginalType} \r\n");
				sb.Append($"\tStack Trace: {msgx.OriginalStackTrace} \r\n\t << remote boundary >> \r\n\t {ex.StackTrace}");
			}
			else
			{
				sb.AppendLine($"\tStack Trace: {ex.StackTrace}");
			}

			if (ex.InnerException != null)
				AppendException(sb, ex.InnerException, true);

			if (ex is AggregateException)
			{
				AggregateException ae = ex as AggregateException;
				if (ae.InnerExceptions.Count > 0)
				{
					sb.AppendLine("\t----- [AggregateException Details START] -----");
					foreach (Exception aeInnerEx in ae.InnerExceptions)
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
			if (useShortVersion)
			{
				if (EntryType == LogEntryType.Exception)
				{
					sb.AppendFormat("EXCEPTION: {0}\r\n", Message);
					sb.AppendLine("Exception Type: " + ExceptionType);
					sb.AppendLine(StackTrace);
				}
				else if (EntryType == LogEntryType.Warning)
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
				if (EntryType == LogEntryType.Exception)
				{
					sb.AppendFormat("Date: {0} - EXCEPTION: {1}\r\n", Date.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss"), Message);
					sb.AppendLine($"\tException Type: {ExceptionType}");
					sb.AppendLine($"\t{StackTrace}");
				}
				else if (EntryType == LogEntryType.Warning)
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
