using System;

namespace Services.Core
{
	public interface ILogFile : IDisposable
	{
		void LogEntry(ILogEntry e);
	}
}
