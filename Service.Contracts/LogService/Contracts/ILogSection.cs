using System;

namespace Services.Core
{
	public interface ILogSection
	{
		string Name { get; }
		void LogMessage(string message, params object[] args);
		void LogWarning(string message, params object[] args);
		void LogWarning(string message, Exception ex);
		void LogException(Exception ex);
		void LogException(string message, Exception ex);
	}
}
