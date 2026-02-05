namespace Services.Core
{
	public class LogServiceLogEntry
	{
		public long Id { get; set; }
		public string EnvironmentName { get; set; } = string.Empty;
		public long DateUtc { get; set; }
		public int EntryType { get; set; }
		public string MachineName { get; set; } = string.Empty;
		public string ComponentName { get; set; } = string.Empty;
		public string InstanceName { get; set; } = string.Empty;
		public int PID { get; set; }
		public int TID { get; set; }
		public string Section { get; set; }
		public string Message { get; set; }
		public string ExceptionType { get; set; }
		public string StackTrace { get; set; }
	}
}
