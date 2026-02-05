using Newtonsoft.Json;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public class AutomatedProcessManager : IAutomatedProcessManager
	{
		class AutomatedProcessInfo
		{
			public IAutomatedProcess Process;
			public DateTime NextTrigger;
		}

		class HandlerRegistrationInfo
		{
			public Type EventType { get; }
			public List<Type> Handlers { get; }

			public HandlerRegistrationInfo(Type eventType)
			{
				EventType = eventType;
				Handlers = new List<Type>();
			}
		}

		private IFactory factory;
		private IEventQueue events;
		private IAppInfo appInfo;
		private IEventSyncStore eventStore;
		private ILogService log;

		private object syncObj = new object();
		private volatile bool started;
		private Timer timer;
		private List<AutomatedProcessInfo> processes;
		private ManualResetEvent waitHandle = new ManualResetEvent(true);
		private Dictionary<Type, HandlerRegistrationInfo> handlerRegistrations;
		private string dbProvider;
		private string dbConnStr;


		public AutomatedProcessManager(IFactory factory)
		{
			this.factory = factory;
			events = factory.GetInstance<IEventQueue>();
			appInfo = factory.GetInstance<IAppInfo>();
			eventStore = factory.GetInstance<IEventSyncStore>();
			log = factory.GetInstance<ILogService>();
			processes = new List<AutomatedProcessInfo>(50);
			handlerRegistrations = new Dictionary<Type, HandlerRegistrationInfo>(50);
		}


		public EventHandler<EQNotification> OnDeadLetter = delegate { };


		public void Setup<T>() where T : class
		{
			var t = typeof(T);
			var instance = factory.GetInstance<T>();
			Reflex.Invoke(instance, "Setup", new object[] { this });
		}


		public void AddProcess<T>() where T : class, IAutomatedProcess
		{
			try
			{
				var element = new AutomatedProcessInfo();
				element.Process = factory.GetInstance<T>();

				var timeout = element.Process.GetIdleTime();
				if (timeout.TotalSeconds < 10)
					log.LogWarning($"Process {typeof(T).Name} returned an invalid IdleTime: IdleTime should not be smaller than 10 seconds. This IdleTime will be ignored and exchanged by a 1 minute delay.");

				processes.Add(element);
			}
			catch (Exception ex)
			{
				log.LogException($"Error initializing automated process of type {typeof(T).FullName}", ex);
			}
		}


		public void AddHandler<EventType, HandlerType>()
			where EventType : EQEventInfo
			where HandlerType : EQEventHandler<EventType>
		{
			Type eventType = typeof(EventType);
			Type handlerType = typeof(HandlerType);
			if (!handlerRegistrations.TryGetValue(eventType, out var registration))
			{
				registration = new HandlerRegistrationInfo(eventType);
				handlerRegistrations.Add(eventType, registration);
			}
			registration.Handlers.Add(handlerType);
		}


		public void Start()
		{
			if (started)
				return;

			EnsureDatabaseIsConfigured();
			foreach (var definition in workflows)
				definition.Validate(workflows);

			var wfm = factory.GetInstance<WFManager>();
			wfm.Start(workflows).Wait();  // TODO: Improve automated process manager to be await/async like WFManager, in that way we dont have to call .Wait(), instead we could await this call...

			started = true;
			StartAutomatedProcesses();
			StartEventHandlers();
			log.LogMessage($"APM system started. Process count: {processes.Count}, Workflow count: {workflows.Count}");
		}


		public void Stop()
		{
			try
			{
				lock (syncObj) { started = false; }

				StopEventHandlers();
				StopAutomatedProcesses();

				var wfm = factory.GetInstance<WFManager>();
				wfm.Stop();
				wfm.WaitForStop().Wait();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		private void StartAutomatedProcesses()
		{
			if (processes.Count > 0)
			{
				foreach (var p in processes)
				{
					try
					{
						p.Process.OnLoad();
					}
					catch (Exception ex)
					{
						log.LogException("Error while calling OnLoad method for automated process...", ex);
					}
				}
				foreach (var p in processes)
				{
					var timeout = p.Process.GetIdleTime();
					if (timeout.TotalSeconds < 10)			// A idle time of less than 10 seconds is not admisible, change that for a sensible default such as 1 minute
						timeout = TimeSpan.FromMinutes(1);

					if (timeout == TimeSpan.MaxValue)
						p.NextTrigger = DateTime.MaxValue;
					else
						p.NextTrigger = DateTime.Now + timeout;
				}
				processes.Sort((p1, p2) => p1.NextTrigger.CompareTo(p2.NextTrigger));
				var t1 = GetTimeToProcess(processes[0]);
				timer = new Timer(CheckProcesses, null, t1, Timeout.Infinite);
			}
		}


		private void StopAutomatedProcesses()
		{
			waitHandle.WaitOne();
			if (timer != null)
				timer.Dispose();
			foreach (var p in processes)
				p.Process.OnUnload();
		}


		private void CheckProcesses(object state)
		{
			lock (syncObj)
			{
				if (!started) return;
				waitHandle.Reset();
			}
			timer.Change(Timeout.Infinite, Timeout.Infinite);
			try
			{
				var p = processes[0];
				do
				{
					ExecuteProcess(p);
					processes.Sort((p1, p2) => p1.NextTrigger.CompareTo(p2.NextTrigger));
					p = processes[0];
				} while (started && p.NextTrigger < DateTime.Now);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				lock (syncObj)
				{
					if (started)
						timer.Change(GetTimeToProcess(processes[0]), Timeout.Infinite);
				}
				waitHandle.Set();
			}
		}


		private void ExecuteProcess(AutomatedProcessInfo p)
		{
			try
			{
				p.Process.OnExecute();
			}
			catch (Exception ex)
			{
				HandleException(p.Process, ex);
			}

			var timeout = p.Process.GetIdleTime();
			if (timeout.TotalSeconds < 10)
				timeout = TimeSpan.FromMinutes(1);

			if (timeout == TimeSpan.MaxValue)
				p.NextTrigger = DateTime.MaxValue;
			else
				p.NextTrigger = DateTime.Now + timeout;
		}


		private void HandleException(IAutomatedProcess proc, Exception ex)
		{
			log.LogException("Automated Process Manager General Error: Unhandled exception while executing automated process", ex);
			var e = new APMErrorNotification() { Message = ex.Message, StackTrace = ex.StackTrace };
			if (proc != null)
			{
				e.HandlerType = proc.GetType().FullName;
				e.NotificationKey = (ex.GetType().ToString() +"-"+ proc.GetType().Name).Length <= 119 ? (ex.GetType().ToString() + "-" + proc.GetType().Name) : (ex.GetType().ToString() + "+" + proc.GetType().Name).Substring(0,119);
			}
			events.Send(e);
			log.LogMessage("APMErrorNotification sent");
		}


		private int GetTimeToProcess(AutomatedProcessInfo p)
		{
			if (p.NextTrigger == DateTime.MaxValue)
				return Timeout.Infinite;
			var t = p.NextTrigger - DateTime.Now;
			if (t.TotalMilliseconds < 0)
				return 0;
			else
				return (int)t.TotalMilliseconds;
		}


		private void EnsureDatabaseIsConfigured()
		{
			var configuration = factory.GetInstance<IAppConfig>();
			dbProvider = configuration.GetValue<string>("Databases.APM.Provider");
			dbConnStr = configuration.GetValue<string>("Databases.APM.ConnStr");
			if (String.IsNullOrWhiteSpace(dbProvider) || String.IsNullOrWhiteSpace(dbConnStr))
				throw new Exception($"APM database is not properly setup in the application settings file: \"{configuration.FileName}\".");

            var model = factory.GetInstance<WorkflowDataModel>();
            model.EnsureDBCreated().Wait();
        }


        private void StartEventHandlers()
		{
			eventStore.OnDeadLetter += (s, e) =>
			{
				try
				{
					OnDeadLetter(this, e);
				}
				catch (Exception ex)
				{
					log.LogException("DeadLetter handler produced an exception.", ex);
				}
			};

			eventStore.Configure(appInfo.AppName + "_APM", dbProvider, dbConnStr);

			foreach (var reg in handlerRegistrations.Values)
			{
				foreach(var handler in reg.Handlers)
				{
					var subscriberid = handler.Name; // TODO: combine namespace with name to avoid naming collitions 
					var subscriptions = new List<EQSubscription>();
					var eventType = reg.EventType;
					var handlerType = handler;
					subscriptions.Add(new EQSubscription(reg.EventType.Name));
					eventStore.UpdateSubscriber(subscriberid, subscriptions);
					eventStore.RegisterHandler(subscriberid, (e) => ProcessNotification(subscriberid, e, eventType, handlerType));
				}
			}
		}


		private void StopEventHandlers()
		{
			eventStore.ClearHandlers();
		}


		private void ProcessNotification(string subscriberid, EQNotification notification, Type eventType, Type handlerType)
		{
			EQEventHandlerResult result;
			try
			{
				log.LogMessage($"Processing event: {eventType.Name}-->{handlerType.Name}");
				var evt = JsonConvert.DeserializeObject(notification.EventData, eventType);
				var instance = factory.GetInstance(handlerType) as EQBaseEventHandler;
				try
				{
					result = instance.HandleEvent(evt);
				}
				finally
				{
					if (instance is IDisposable)
						(instance as IDisposable).Dispose();
				}
				if (result.Success)
				{
					log.LogMessage($"Processing event: {eventType.Name}-->{handlerType.Name}, Handler returned Success as true, event will be dismissed.");
					eventStore.AcknowledgeNotification(subscriberid, notification.ID);
				}
				else
				{
                    log.LogMessage($"Processing event: {eventType.Name}-->{handlerType.Name}, Handler returned Success as false, event will be delayed.");
                    eventStore.UpdateEventState(notification.ID, JsonConvert.SerializeObject(evt));
                    eventStore.DelayNotification(subscriberid, notification.ID, ClipDelay(result.Delay, notification.RetryCount));
				}
			}
			catch (Exception ex)
			{
				log.LogException($"Processing event: {eventType.Name}-->{handlerType.Name}, Error while processing event, event will be delayed.", ex);
				try
				{
					eventStore.DelayNotification(subscriberid, notification.ID, ClipDelay(TimeSpan.Zero, notification.RetryCount));
				}
				catch (Exception ex2)
				{
					log.LogException("Error while delaying event...", ex2);
				}

				var e = new APMErrorNotification()
				{
                    NotificationKey = notification.EventData.GetHashCode().ToString(),
					Message = ex.Message,
					StackTrace = ex.StackTrace,
					HandlerType = handlerType.FullName,
					Data = JsonConvert.DeserializeObject(notification.EventData, eventType)
				};
				events.Send(e);
				log.LogMessage("APMErrorNotification sent");
			}
		}


		// NOTE: Delay cannot be less than 10 seconds or greater than 24 hours, if it is, we clip. 
		// Additionally if the specified delay is TimeSpan.Zero, then the delay is calculated based on
		// the number of times the event has failed (up to 1 hr).
		private TimeSpan ClipDelay(TimeSpan delay, int retryCount)
		{
			if (delay == TimeSpan.Zero)
			{
				int delayTimeInMinutes = 1 + retryCount * 2;
				delay = TimeSpan.FromMinutes(delayTimeInMinutes);
			}

			if (delay.TotalMinutes < 1)
				return TimeSpan.FromMinutes(1);
			else if (delay.TotalMinutes > 60)
				return TimeSpan.FromMinutes(60);
			else
				return delay;
		}


		// ==============================================================================================================
		// Workflow API
		// ==============================================================================================================
		#region Workflow API

		private ConcurrentDictionary<string, WorkflowDefinition> index = new ConcurrentDictionary<string, WorkflowDefinition>();
		private List<WorkflowDefinition> workflows = new List<WorkflowDefinition>();

		public IWorkflowDefinition<TItem> AddWorkflow<TItem>(string workflowName) where TItem : WorkItem
		{
			if (String.IsNullOrWhiteSpace(workflowName))
				throw new ArgumentNullException(nameof(workflowName));
			if (started)
				throw new InvalidOperationException("Cannot define additional workflows once the Automated Process Manager has been started.");

			var wf = new WorkflowDefinition<TItem>(workflowName);

			string key = wf.Name.ToLower().Trim();
			if (!index.TryAdd(key, wf))
				throw new InvalidOperationException($"Workflow \"{wf.Name}\" has already been defined.");
			workflows.Add(wf);

			return wf;
		}

		public async Task<IEnumerable<IWorkflow>> GetWorkflowsAsync()
		{
			var wfm = factory.GetInstance<WFManager>();
			return await wfm.GetWorkflows();
		}

		public async Task<IWorkflow> GetWorkflowAsync(int id)
		{
			var wfm = factory.GetInstance<WFManager>();
			return await wfm.GetWorkflow(id);
		}

		public async Task<IWorkflow> GetWorkflowAsync(string name)
		{
			var wfm = factory.GetInstance<WFManager>();
			return await wfm.GetWorkflow(name);
		}
		#endregion
	}
}
