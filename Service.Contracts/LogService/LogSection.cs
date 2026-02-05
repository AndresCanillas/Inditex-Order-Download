using System;

namespace Services.Core
{
	public class LogSection : ILogSection
	{
		private readonly ILogService log;
		private readonly string sectionName;

		internal LogSection(ILogService log, string sectionName)
		{
			sectionName = sectionName.SanitizeFileName();

			this.log = log;
			this.sectionName = sectionName;
		}

		public string Name => sectionName;

		public void LogMessage(string message, params object[] args)
		{
			var entry = new LogEntry(string.Format(message, args));
			entry.Section = sectionName;
			log.LogEntry(entry);
		}

		public void LogWarning(string message, params object[] args)
		{
			var entry = new LogEntry(string.Format(message, args), null, LogEntryType.Warning);
			entry.Section = sectionName;
			log.LogEntry(entry);
		}

		public void LogWarning(string message, Exception ex)
		{
			var entry = new LogEntry(message, ex, LogEntryType.Exception);
			entry.Section = sectionName;
			log.LogEntry(entry);
		}

		public void LogException(Exception ex)
		{
			var entry = new LogEntry("", ex, LogEntryType.Exception);
			entry.Section = sectionName;
			log.LogEntry(entry);
		}

		public void LogException(string message, Exception ex)
		{
			var entry = new LogEntry(message, ex, LogEntryType.Exception);
			entry.Section = sectionName;
			log.LogEntry(entry);
		}
	}
}
