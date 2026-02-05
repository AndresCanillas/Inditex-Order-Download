using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Core
{
	class LogService : BaseServiceClient, ILogService
	{
		const string LOG_SERVICE_EXCEPTION = "-LogServiceException-";

        private readonly IAppInfo appInfo;
        private readonly string baseDir;
        private readonly LogFile localLogFile;
        private readonly SQDataFile<Logs> logs;
        private readonly SQDataFile<Metrics> metrics;
        private readonly string apiKey = string.Empty;
		private readonly string environmentName = string.Empty;
		private readonly string machineName = string.Empty;
		private readonly string componentName = string.Empty;
		private readonly string instanceName = string.Empty;
        private readonly CancellationTokenSource cts;
        private readonly Task saveEntriesTask;
        private readonly Task synchronizeTask;
        private readonly ConcurrentBag<ILogEntry> pendingLogEntries = new ConcurrentBag<ILogEntry>();
        private readonly ConcurrentBag<BaseMetric> pendingMetrics = new ConcurrentBag<BaseMetric>();
        private readonly ConcurrentDictionary<string, LogFile> sections = new ConcurrentDictionary<string, LogFile>();

        public LogService(IAppInfo appInfo, IAppConfig config)
			: base(config.GetValue<string>("LogService.Server"), "log")
		{
            this.appInfo = appInfo;
            cts = new CancellationTokenSource();
			try
			{
                var filePath = Path.Combine(appInfo.SystemLogDir, "LogService.log");
                localLogFile = new LogFile(filePath);

                machineName = Environment.MachineName;
                apiKey = config.GetValue("LogService.ApiKey", "");
                environmentName = config.GetValue("LogService.EnvironmentName", "");
                componentName = config.GetValue("LogService.ComponentName", "");
                instanceName = config.GetValue("LogService.InstanceName", "");

                baseDir = Path.Combine(appInfo.SystemLogDir, "LogService");
                AssignDirectoryPermissions(baseDir);
                DeleteOldVersionDataFiles(baseDir);

                var logsFilePath = Path.Combine(baseDir, "logs2.dat");
                logs = new SQDataFile<Logs>(logsFilePath);

                var metricsFilePath = Path.Combine(baseDir, "metrics2.dat");
                metrics = new SQDataFile<Metrics>(metricsFilePath);

				AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
				{
					var ex = e.ExceptionObject as Exception;
					if(ex != null)
						LogEntry(new LogEntry(ex));
				};

				TaskScheduler.UnobservedTaskException += (sender, e) =>
				{
					e.SetObserved();
					LogEntry(new LogEntry(e.Exception));
				};

                saveEntriesTask = Task.Run(() => SaveEntriesAsync(cts.Token));
                synchronizeTask = Task.Run(() => SynchronizeEntriesAsync(cts.Token));
            }
            catch(Exception ex)
			{
				if(localLogFile != null)
					localLogFile.LogEntry(new LogEntry(LOG_SERVICE_EXCEPTION, ex));
				else
					throw;
			}
		}

        private void DeleteOldVersionDataFiles(string baseDir)
        {
            var logsFilePath = Path.Combine(baseDir, "logs.dat");
            var metricsFilePath = Path.Combine(baseDir, "metrics.dat");
            try { File.Delete(logsFilePath); }
            catch { } // Ignore error
            try { File.Delete(metricsFilePath); }
            catch { } // Ignore error
        }

        public string FilePath
			=> localLogFile?.FilePath;

		public bool EnableConsoleLog { get; set; }

        private bool CentralLogServiceEnabled
            => !string.IsNullOrWhiteSpace(BaseUri) && logs != null && metrics != null;

        public ILogSection GetSection(string name)
			=> new LogSection(this, name);

        public void LogMessage(string message, params object[] args)
        {
            if(args != null && args.Length > 0)
                LogEntry(new LogEntry(string.Format(message, args)));
            else
                LogEntry(new LogEntry(message));
        }

        public void LogWarning(string message, params object[] args)
        {
            if(args != null && args.Length > 0)
                LogEntry(new LogEntry(string.Format(message, args), LogEntryType.Warning));
            else
                LogEntry(new LogEntry(message, LogEntryType.Warning));
        }

		public void LogWarning(string message, Exception ex)
			=> LogEntry(new LogEntry(message, ex, LogEntryType.Warning));

		public void LogException(Exception ex)
			=> LogEntry(new LogEntry(ex));

		public void LogException(string message, Exception ex = null)
			=> LogEntry(new LogEntry(message, ex));

		public void LogEntry(ILogEntry e)
		{
            // Exit if log service is not configured
            if(string.IsNullOrWhiteSpace(BaseUri))
                return;

            pendingLogEntries.Add(e);
        }

        public void AddMetric<T>(T metric) where T : BaseMetric
        {
            // Exit if log service is not configured
            if(string.IsNullOrWhiteSpace(BaseUri))
                return;

            pendingMetrics.Add(metric);
        }

        public void Terminate()
        {
            // Exit if log service is not configured
            if(string.IsNullOrWhiteSpace(BaseUri))
                return;

            cts.Cancel();
            try
            {
                saveEntriesTask?.Wait();
                synchronizeTask?.Wait();
            }
            catch(TaskCanceledException)
            {
                // Ignore this exception, it is expected when the service is stopped.
            }
            catch(Exception ex)
            {
                LogException(ex);
            }
        }

        public async Task TerminateAsync()
        {
            // Exit if log service is not configured
            if(string.IsNullOrWhiteSpace(BaseUri))
                return;

            cts.Cancel();
            try
            {
                if(saveEntriesTask != null)
                    await saveEntriesTask;
                if(synchronizeTask != null)
                    await synchronizeTask;
            }
            catch(TaskCanceledException)
            {
                // Ignore this exception, it is expected when the service is stopped.
            }
            catch(Exception ex)
            {
                LogException(ex);
            }
        }


        // Background task that saves entries and metrics to disk, executes frequently to prevent data loss.
        // Meant so data can survive a process restart without losing information.
        private async Task SaveEntriesAsync(CancellationToken cancellationToken)
        {
            if(CentralLogServiceEnabled)
            {
                var t1 = Task.Run(() => logs.Repair(cancellationToken));
                var t2 = Task.Run(() => metrics.Repair(cancellationToken));
                await Task.WhenAll(t1, t2);
                if(cancellationToken.IsCancellationRequested)
                    return;
            }

            int errorCount = 0;
            int terminateCicles = 0;
            do
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    // This prevents the process from looping indefinitely in an scenario where we still have entries to save but are requested to stop.
                    // The objective here is to persist all entries before stopping, while ensuring we exit at some point (after 3 loops).
                    terminateCicles++;
                    if(terminateCicles > 3)
                        return;
                }

                // Wait only if the queue is empty
                if(pendingLogEntries.Count == 0)
                {
                    if(cancellationToken.IsCancellationRequested)
                        await Task.Delay(100);  // Small wait (disregarding cancellation token)
                    else
                        await cancellationToken.WaitHandle.WaitOneAsync(TimeSpan.FromMilliseconds(500), CancellationToken.None);  // Normal wait of 500 ms on cancellationToken
                }

                try
                {
                    // Take log entries out of the queue
                    List<Logs> logEntries = new List<Logs>();
                    while(pendingLogEntries.TryTake(out var entry))
                    {
                        var logEntry = PreProcessEntry(entry);
                        logEntries.Add(logEntry);
                    }

                    // Take metrics out of the queue
                    List<Metrics> metricEntries = new List<Metrics>();
                    while(pendingMetrics.TryTake(out var metric))
                    {
                        var entry = PrepareMetric(metric);
                        var json = JsonConvert.SerializeObject(entry);
                        metricEntries.Add(new Metrics(json));
                    }

                    // Write entries to local data files.
                    try
                    {
                        if(logEntries.Count > 0 && CentralLogServiceEnabled)
                            logs.WriteEntries(logEntries);
                    }
                    catch(Exception ex)
                    {
                        if(errorCount % 120 == 0)
                            localLogFile?.LogEntry(new LogEntry(LOG_SERVICE_EXCEPTION, ex, LogEntryType.Exception));
                        errorCount++;
                    }

                    try
                    {
                        if(metricEntries.Count > 0 && CentralLogServiceEnabled)
                            metrics.WriteEntries(metricEntries);
                    }
                    catch(Exception ex)
                    {
                        if(errorCount % 120 == 0)
                            localLogFile?.LogEntry(new LogEntry(LOG_SERVICE_EXCEPTION, ex, LogEntryType.Exception));
                        errorCount++;
                    }
                }
                catch(Exception ex)
                {
                    if(errorCount % 120 == 0)
                        localLogFile?.LogEntry(new LogEntry(LOG_SERVICE_EXCEPTION, ex, LogEntryType.Exception));
                    errorCount++;
                }
            } while(true);
        }


        private Logs PreProcessEntry(ILogEntry e)
        {
            LogToLocalFileAndConsole(e);
            var dateUtc = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
            var entryType = TranslateEntryType(e.EntryType);
            return new Logs(environmentName, machineName, componentName, instanceName, dateUtc, entryType, e.PID, e.TID, e.Section, e.Message, e.ExceptionType, e.StackTrace, e.UserName, e.RemoteHost);
        }

        private void LogToLocalFileAndConsole(ILogEntry e)
        {
            if(e == null)
                return;

            try
            {
                if(string.IsNullOrWhiteSpace(e.Section))
                    localLogFile?.LogEntry(e);
                else
                    LogSectionEntry(e);
            }
            catch { } // Logging to local text file generated an exception, not much we can do: Just prevent exception from crashing the process

            try
            {
                if(EnableConsoleLog)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = GetColorFromEntryType(e.EntryType);
                    Console.WriteLine(e);
                    Console.ForegroundColor = color;
                }
            }
            catch { } // Logging to console generated an exception, not much we can do: Just prevent exception from crashing the process
        }


        private void LogSectionEntry(ILogEntry e)
        {
            if(e.Section == null)
                return;
            var logFile = sections.GetOrAdd(e.Section, section =>
            {
                var filePath = Path.Combine(appInfo.SystemLogDir, $"LogService.{section}.log".SanitizeFileName());
                return new LogFile(filePath);
            });

            logFile.LogEntry(e);
        }

        private ConsoleColor GetColorFromEntryType(LogEntryType entryType)
        {
            switch(entryType)
            {
                case LogEntryType.Exception: return ConsoleColor.Red;
                case LogEntryType.Warning: return ConsoleColor.Yellow;
                default: return ConsoleColor.White;
			}
        }

        private int TranslateEntryType(LogEntryType entryType)
		{
			switch(entryType)
			{
				case LogEntryType.Message:
					return 0;
				case LogEntryType.Warning:
					return 1;
				default:
					return 2;
			}
		}

        private LogServiceMetricEntry PrepareMetric<T>(T metric) where T : BaseMetric
        {
            metric.EnvironmentName = environmentName;
            metric.MachineName = machineName;
            metric.ComponentName = componentName;
            metric.InstanceName = instanceName;
            metric.DateUtc = DateTime.UtcNow;
            return metric.MapMetric();
        }


        // Background task that sends entries to the central LogServer (executes every 10 seconds)
        // Since data pending synchornization is stored locally in a data file, ILogService can gracefully
        // handle networking issues, service unavailability and process restarts.
        // NOTE: Process can still keep sending entries to the remote server when asked to stop by the CancellationToken,
        //       this is done so we push as many log entries as possible before the process stops.
        private async Task SynchronizeEntriesAsync(CancellationToken cancellationToken)
        {
            // Exit if log service is not configured
            if(string.IsNullOrWhiteSpace(BaseUri))
				return;

            int terminateCicles = 0;
            do
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    // This prevents the process from looping indefinitely in an scenario where we are requested to stop, but there are still entries
                    // pending synchornization. The objective here is to send as many entries as posible to the server before stopping, while
                    // ensuring we exit at some point (after 'at most' 10 loops).
                    terminateCicles++;
                    if(terminateCicles > 10)
                        return;
                }

                try
                {
                    var data = new LogServiceEntries();
                    var logsReadResult = logs.ReadEntries(1000);
                    var metricsReadResult = metrics.ReadEntries(1000);
                    data.LogEntries = logsReadResult.Records;
                    data.MetricEntries = metricsReadResult.Records;

                    if(data.Any())
                    {
                        // Send entries to the server
                        await PostAsync(
                            "sync",
                            data,
                            new Dictionary<string, string>() { { "ApiKey", apiKey } });

                        logs.DiscardEntries(logsReadResult);
                        metrics.DiscardEntries(metricsReadResult);
                    }
                    else
                    {
                        // No more entries: Check if we need to exit or keep looping after a small delay
                        if(cancellationToken.IsCancellationRequested)
                            return;
                        else
                            await cancellationToken.WaitHandle.WaitOneAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                    }
                }
                catch(Exception ex)
                {
                    localLogFile?.LogEntry(new LogEntry(LOG_SERVICE_EXCEPTION, ex));

                    // After an error: Exit if cancellation requested, otherwise wait for 30 secs before looping
                    if(cancellationToken.IsCancellationRequested)
                        return;
                    else
                        await cancellationToken.WaitHandle.WaitOneAsync(30000, CancellationToken.None);
                }
            } while(true);
        }


        private void AssignDirectoryPermissions(string baseDir)
        {
            try
            {
                if(!Directory.Exists(baseDir))
                    Directory.CreateDirectory(baseDir);

                var di = new DirectoryInfo(baseDir);
                var sec = di.GetAccessControl();
                var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                sec.AddAccessRule(new FileSystemAccessRule(
                    sid, FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));
                di.SetAccessControl(sec);
            }
            catch(Exception ex)
            {
                localLogFile?.LogEntry(new LogEntry(LOG_SERVICE_EXCEPTION, ex));
            }
        }
	}
}
