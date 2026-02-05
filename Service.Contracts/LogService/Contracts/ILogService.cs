using System;
using System.Threading.Tasks;

namespace Services.Core
{
	public interface ILogService
	{
		string FilePath { get; }

		bool EnableConsoleLog { get; set; }

		ILogSection GetSection(string name);

		void LogMessage(string message, params object[] args);
		void LogWarning(string message, params object[] args);
		void LogWarning(string message, Exception ex);
		void LogException(Exception ex);
		void LogException(string message, Exception ex = null);
		void LogEntry(ILogEntry entry);
		void AddMetric<T>(T metric) where T : BaseMetric;

		/// <summary>
		/// Calling Terminate (or TerminateAsync) is not mandatory, but it is recomended that the application code calls any of these methods before the process shuts down to ensure proper completion of any pending background task related to logging.
		/// </summary>
		/// <remarks>
		/// IMPORTANT: Call Terminate as the last step in your shutdown sequence, after all other functions such as web servers or background workers have been properly shutdown. Calling Terminate prematurely can cause some log entries to be syncrhonized with the central log server until the next time the process gets run, which can cause confusion.
		/// </remarks>
		void Terminate();

		/// <summary>
		/// Calling Terminate (or TerminateAsync) is not mandatory, but it is recomended that the application code calls any of these methods before the process shuts down to ensure proper completion of any pending background task related to logging. 
		/// </summary>
		/// <remarks>
		/// IMPORTANT: Call Terminate as the last step in your shutdown sequence, after all other functions such as web servers or background workers have been properly shutdown. Calling Terminate prematurely can cause some log entries to be syncrhonized with the central log server until the next time the process gets run, which can cause confusion.
		/// </remarks>
		Task TerminateAsync();
	}
}
