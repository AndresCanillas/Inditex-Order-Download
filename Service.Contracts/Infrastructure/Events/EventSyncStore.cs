using Newtonsoft.Json;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
    /* ====================================================================================================
	 * EventSyncStore
	 * 
	 * Its main purpose is to ensure that events that are of interest to 3rd parties are not lost if
	 * said events are detected while the intended subscribers are offline, or if the application is
	 * restarted.
	 * 
	 * In order to achieve that purpose, subscribers and the events they are interested in receiving,
	 * are registered in a database. When the service is configured, all registrations are loaded from
	 * the database, and the code necesary to "capture" events of interest is setup.
	 * 
	 * Other components depend on this service in order to retrieve events that have not been successfully
	 * delivered and to acknowledge events whose delivery has been confirmed.
	 * ==================================================================================================== */
    public class EventSyncStore : IEventSyncStore
    {
        private object syncObj = new object();
        private string dbName;
        private IDBConfiguration db;
        private IEventQueue events;
        private ILogSection log;
        private ConcurrentDictionary<int, EQEventSubscriptorInfo> liveSubscriptors;
        private ConcurrentQueue<EQEventInfo> eventsRegistered;
        private EQService service;
        private CancellationTokenSource cts;
        private IAppConfig config;
        private readonly int maxDegreeOfParallelism;


        public EventSyncStore(IDBConfiguration db, IEventQueue events, ILogService log, IAppConfig config)
        {
            this.db = db;
            this.events = events;
            this.cts = new CancellationTokenSource();
            this.log = log.GetSection("EventSyncStore");
            liveSubscriptors = new ConcurrentDictionary<int, EQEventSubscriptorInfo>();
            eventsRegistered = new ConcurrentQueue<EQEventInfo>();
            this.config = config;
            maxDegreeOfParallelism = config.GetValue("EventSyncService.MaxDegreeOfParallelism", 3);
            if(maxDegreeOfParallelism < 1) maxDegreeOfParallelism = 1;
            if(maxDegreeOfParallelism > 5) maxDegreeOfParallelism = 5;

        }


        public event EventHandler<EQNotification> OnDeadLetter;


        public void Configure(string serviceName, string provider, string connStr)
        {
            lock(syncObj)
            {
                db.ProviderName = provider;
                db.ConnectionString = connStr;
                dbName = db.GetInitialCatalog();
                db.EnsureCreated();
                log.LogMessage($"EventSyncStore configured to connecto to: {dbName}");
                using(var conn = db.CreateConnection())
                {
                    UpdateDBObjects(conn);
                    BindService(conn, serviceName);
                    LoadLiveSubscriptors(conn);
                    events.OnEventRegistered += HandleEventRegistered;
                    Task.Factory.StartNew(PersistEvents, TaskCreationOptions.LongRunning);
                    Task.Factory.StartNew(SendNotifications, TaskCreationOptions.LongRunning);
                }
            }
        }


        private void BindService(IDBX conn, string serviceName)
        {
            service = conn.SelectOne<EQService>("select * from EQService where name = @sname", serviceName);
            if(service == null)
            {
                service = new EQService() { Name = serviceName };
                conn.Insert(service);
            }
            // Bind any lose events/subscribers to the newly created service
            conn.ExecuteNonQuery("update EQEvent set ServiceID = @serviceid where ServiceID is null", service.ID);
            conn.ExecuteNonQuery("update EQSubscriber set ServiceID = @serviceid where ServiceID is null", service.ID);
        }


        private void LoadLiveSubscriptors(IDBX conn)
        {
            var subsciptors = conn.Select<EQSubscriber>("select * from EQSubscriber where ServiceID = @serviceid", service.ID);
            foreach(var sub in subsciptors)
            {
                liveSubscriptors[sub.ID] = new EQEventSubscriptorInfo(
                    sub.ID,
                    sub.SubscriberID,
                    sub.ActiveSubscriptions.Split(',', (s) => new EQSubscription(s)),
                    sub.LastSeenOnline);
            }
        }


        private void HandleEventRegistered(EQEventInfo evt)
        {
            //IMPORTANT: Must avoid processing events that are not generated locally.
            if(evt.Source == EventSource.Local)
            {
                eventsRegistered.Enqueue(evt);
            }
        }


        protected async Task PersistEvents()
        {
            do
            {
                while(eventsRegistered.TryDequeue(out var evt))
                {
                    try
                    {
                        var evtData = JsonConvert.SerializeObject(evt);
                        using(var conn = db.CreateConnection())
                        {
                            foreach(var subscriber in liveSubscriptors)
                            {
                                if(subscriber.Value.IsSubscribed(evt))
                                {
                                    await conn.ExecuteNonQueryAsync(@"
									select @evtId = ID from EQEvent
									where EventName = @evtName and EventData = @evtData and ServiceID = @serviceId

									if @evtId is null
									begin
										insert into EQEvent values(@evtName, @evtData, getdate(), @serviceId)
										set @evtId = SCOPE_IDENTITY()
									end

									if not Exists(select 1 from EQTracking where SubscriberID = @subscriberId and EventID = @evtId)
									begin
										insert into EQTracking values (@subscriberId, @evtId, 0, null, 0)
									end", null, evt.EventName, evtData, service.ID, subscriber.Value.ID);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        log.LogException(ex);
                    }
                }

                if(!cts.Token.IsCancellationRequested)
                    await Task.Delay(250);

            } while(!cts.Token.IsCancellationRequested);
        }


        private async Task SendNotifications(object state)
        {
            int count = 0;

            var parallelOptions = new ParallelOptions()
            {
                CancellationToken = cts.Token,
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            while(!cts.Token.IsCancellationRequested)
            {
                if(count % 100 == 0)
                    RemoveEventsOverRetentionThreshold();

                count++;

                try
                {
                    bool foundEvents = false;
                    do
                    {
                        var pendingEvents = GetPendingEvents();

                        Parallel.ForEach(pendingEvents, parallelOptions, (evt) =>
                        {
                            if(liveSubscriptors.TryGetValue(evt.SubscriberID, out var subscriber))
                            {
                                if(subscriber.NotificationHandlers != null)
                                {
                                    // Subscriber is online, attempt delivery...
                                    try
                                    {
                                        MarkAsSent(evt.SubscriberID, evt.ID);
                                        var e = new EQNotification(evt.ID, evt.EventName, evt.EventData, evt.Date, evt.RetryCount);
                                        foreach(var handler in subscriber.NotificationHandlers)
                                            handler(e);
                                    }
                                    catch(Exception ex)
                                    {
                                        log.LogException(ex);
                                        MarkAsDelayed(evt.SubscriberID, evt.ID, DateTime.Now.AddMinutes(1), true);
                                        CheckDeadLetter(evt);
                                    }
                                }
                                else
                                {
                                    // Subscriber is not online, check if we should delay notification or delete it, based on how long this event has being lingering in the system.
                                    if(evt.Date.AddDays(5) > DateTime.Now)
                                        MarkAsDelayed(evt.SubscriberID, evt.ID, DateTime.Now.AddMinutes(2), false);
                                    else
                                        DeleteNotification(evt.SubscriberID, evt.ID);
                                }
                            }
                            else
                            {
                                // There is no subscriber for this notification, delete it
                                DeleteNotification(evt.SubscriberID, evt.ID);
                            }
                        });

                        foundEvents = pendingEvents.Count > 0;
                    } while(foundEvents);
                }
                catch(ThreadAbortException)
                {
                    log.LogMessage($"EventSyncStore {dbName}: SendNotifications ThreadAborted");
                    return;
                }
                catch(Exception ex)
                {
                    log.LogException(ex);
                }

                if(!cts.Token.IsCancellationRequested)
                    await Task.Delay(5000, cts.Token);
            }
        }

        private void RemoveEventsOverRetentionThreshold()
        {
            try
            {
                // Delete events that exceed retention threshold
                using(var conn = db.CreateConnection())
                {
                    conn.ExecuteNonQuery(@"
                    DELETE FROM EQTracking
                    WHERE EXISTS (
                        SELECT 1
                        FROM EQTracking t
                        INNER JOIN EQEvent e ON e.ID = t.EventID
                        INNER JOIN EQSubscriber s ON s.ID = t.SubscriberID
                        WHERE 
                            e.Date < DATEADD(DAY, -ISNULL(s.RetentionThreshold, 30), GETDATE())
                            AND EQTracking.SubscriberID = t.SubscriberID
                            AND EQTracking.EventID = t.EventID
                    )");

                    conn.ExecuteNonQuery(@"
                        DELETE FROM EQEvent 
                        WHERE NOT EXISTS(
                            SELECT EventID
                            FROM EQTracking 
                            WHERE EventID = EQEvent.ID
                        )
                        AND EQEvent.Date < DATEADD(DAY, -1, GETDATE())
                    ");
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
            }
        }

        private List<EQEventProj> GetPendingEvents()
        {
            using(var conn = db.CreateConnection())
            {
                return conn.Select<EQEventProj>(@"
                    SELECT TOP 30 e.*, t.SubscriberID, t.RetryCount 
						FROM EQEvent e
						JOIN EQTracking t ON t.EventID = e.ID
					WHERE
						e.ServiceID = @serviceid AND
						t.EventSent = 0 AND
						(t.DelayedUntil IS null OR t.DelayedUntil < @date)
					ORDER BY e.[Date]",
                    service.ID, DateTime.Now);
            }
        }


        private void MarkAsSent(int subscriberid, int eventid)
        {
            using(var conn = db.CreateConnection())
            {
                log.LogMessage($"EventSyncStore {dbName}: Event Marked As Sent, Service: {service.Name}, EventID: {eventid}, SubscriberID: {subscriberid}");
                conn.ExecuteNonQuery(@"
				update EQTracking set EventSent = 1 
				where 
					SubscriberID = @sid and
					EventID = @evtid",
                    subscriberid, eventid);
            }
        }


        private void MarkAsDelayed(int subscriberid, int eventid, DateTime delayedUntil, bool countAsRetry)
        {
            using(var conn = db.CreateConnection())
            {
                log.LogMessage($"EventSyncStore {dbName}: Event Marked As Delayed, Service: {service.Name}, EventID: {eventid}, SubscriberID: {subscriberid}, DelayedUntil: {delayedUntil}");

                var retryCountFragment = countAsRetry ? ", RetryCount = ISNULL(RetryCount, 0) + 1" : "";

                conn.ExecuteNonQuery($@"
				update EQTracking set EventSent = 0, DelayedUntil = @date {retryCountFragment}
				where
					SubscriberID = @sid and
					EventID = @evtid",
                        delayedUntil, subscriberid, eventid);
            }
        }


        private void CheckDeadLetter(EQEventProj evt)
        {
            evt.RetryCount++;
            if(evt.RetryCount > 10)
            {
                try
                {
                    if(OnDeadLetter != null)
                    {
                        var e = new EQNotification(evt.ID, evt.EventName, evt.EventData, evt.Date, evt.RetryCount);
                        OnDeadLetter(this, e);
                    }
                    DeleteNotification(evt.SubscriberID, evt.ID);
                }
                catch(Exception ex)
                {
                    log.LogException(ex);
                }
            }
        }


        public void UpdateSubscriber(string subscriberid, List<EQSubscription> subscriptions)
        {
            using(var conn = db.CreateConnection())
            {

                var subscriber = conn.SelectOne<EQSubscriber>(@"
					select * from EQSubscriber
					where
						SubscriberID = @subscriberid and
						ServiceID = @serviceid",
                    subscriberid, service.ID);

                if(subscriber == null)
                {
                    // This is the first time this subscriber is seen
                    subscriber = new EQSubscriber()
                    {
                        SubscriberID = subscriberid,
                        ActiveSubscriptions = subscriptions.Merge(",", (s) => s.EventName),
                        LastSeenOnline = DateTime.Now,
                        ServiceID = service.ID
                    };
                    conn.Insert(subscriber);
                }
                else
                {
                    // This is an existing subscriptor, first remove events that might no longer be of interest:
                    var oldSubscriptions = subscriber.ActiveSubscriptions.Split(',', (s) => new EQSubscription(s));
                    foreach(var oldSub in oldSubscriptions)
                    {
                        if(subscriptions.FindIndex((s) => s.EventName == oldSub.EventName) < 0)
                        {
                            conn.ExecuteNonQuery(@"
							delete t from EQTracking t
							join EQEvent ev on ev.ID = t.EventID
							where
								t.SubscriberID = @sid and
								ev.EventName = @evtname
							", subscriber.ID, oldSub.EventName);
                        }
                    }

                    // Then update the subscriptor registration
                    subscriber.LastSeenOnline = DateTime.Now;
                    subscriber.ActiveSubscriptions = subscriptions.Merge(",", (s) => s.EventName);
                    conn.Update(subscriber);

                    // Reset all events that have not been acknowledged (acknowledged events are removed from the EQTracking table)
                    conn.ExecuteNonQuery("update EQTracking set EventSent = 0 where SubscriberID = @sid", subscriber.ID);
                }

                // Lastly update liveSubscriptors
                if(!liveSubscriptors.TryGetValue(subscriber.ID, out var subInfo))
                {
                    subInfo = new EQEventSubscriptorInfo(subscriber.ID, subscriberid, subscriptions, subscriber.LastSeenOnline);
                    liveSubscriptors.TryAdd(subscriber.ID, subInfo);
                }
                else
                {
                    subInfo.Update(subscriptions, subscriber.LastSeenOnline);
                }
            }
        }


        public void RemoveSubscriber(string subscriberid)
        {
            using(var conn = db.CreateConnection())
            {
                var subscriber = FindSubscriber(subscriberid);
                if(subscriber != null)
                {
                    liveSubscriptors.TryRemove(subscriber.ID, out _);
                    // Delete all event tracking for the deleted subscriber
                    conn.ExecuteNonQuery("delete from EQTracking where SubscriberID = @subscriberid", subscriber.ID);
                    // Delete all events that do not have at least one related record in EQTracking (i.e. events no one will ever receive in the future)
                    conn.ExecuteNonQuery("delete ev from EQEvent ev left outer join EQTracking evt on ev.ID = evt.EventID where evt.EventID is null");
                    // Delete subscriber
                    conn.Delete(subscriber);
                }
            }
        }


        public void RegisterHandler(string subscriberid, Action<EQNotification> action)
        {
            var subscriber = FindSubscriber(subscriberid);
            if(subscriber != null)
            {
                if(!subscriber.NotificationHandlers.Contains(action))
                    subscriber.NotificationHandlers.Add(action);
            }
            else
                throw new InvalidOperationException($"Cannot register event handler because subscriber {subscriberid} was not found.");
        }


        public void RemoveHandler(string subscriberid, Action<EQNotification> action)
        {
            var subscriber = FindSubscriber(subscriberid);
            if(subscriber != null)
            {
                subscriber.NotificationHandlers.Remove(action);
            }
        }


        public void ClearHandlers()
        {
            // cancel loop with CTS
            cts.Cancel();

            foreach(var subscriber in liveSubscriptors.Values)
            {
                subscriber.NotificationHandlers.Clear();
            }
        }


        private void DeleteNotification(int subscriberid, int eventid)
        {
            using(var conn = db.CreateConnection())
            {
                conn.ExecuteNonQuery(@"
					delete from EQTracking where SubscriberID = @sid and EventID = @evtid
					if not exists(select EventID from EQTracking where EventID = @evtid)
						delete from EQEvent where ID = @evtid", subscriberid, eventid);
                //if (!conn.Exists("select * from EQTracking where EventID = @evtid", eventid))
                //{
                //	conn.ExecuteNonQuery("delete from EQEvent where ID = @evtid", eventid);
                //}
            }
        }


        public void AcknowledgeNotification(string subscriberid, int eventid)
        {
            try
            {
                var subscriber = FindSubscriber(subscriberid);
                if(subscriber != null)
                {
                    DeleteNotification(subscriber.ID, eventid);
                    log.LogMessage($"EventSyncStore {dbName}: AcknowledgeNotification, Service: {service.Name}, SubscriberID: {subscriberid}, EventID: {eventid}, Event was completed.");
                }
                else
                {
                    log.LogMessage($"EventSyncStore {dbName}: AcknowledgeNotification, could not find subscriber {subscriberid}.");
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
            }
        }


        public void UpdateEventState(int eventid, string eventState)
        {
            try
            {
                using(var conn = db.CreateConnection())
                {
                    conn.ExecuteNonQuery(@"
						update EQEvent set EventData = @eventState
						where 
							ID = @eventid
						", eventState, eventid);
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
            }
        }


        public void DelayNotification(string subscriberid, int eventid, TimeSpan delay)
        {
            try
            {
                var subscriberInfo = FindSubscriber(subscriberid);
                if(subscriberInfo != null)
                {
                    MarkAsDelayed(subscriberInfo.ID, eventid, DateTime.Now.Add(delay), true);
                }
                else
                {
                    log.LogMessage($"EventSyncStore {dbName}: DelyaNotification, could not find subscriber {subscriberid}");
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
            }
        }

        private EQEventSubscriptorInfo FindSubscriber(string subscriberid)
        {
            var kvp = liveSubscriptors.FirstOrDefault(p => p.Value != null && p.Value.SubscriberID == subscriberid);
            return kvp.Value;
        }

        private void UpdateDBObjects(IDBX conn)
        {
            conn.ExecuteNonQuery(@"
				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EQEvent]') AND type in (N'U'))
				BEGIN
				CREATE TABLE [dbo].[EQEvent](
					[ID] [int] IDENTITY(1,1) NOT NULL,
					[EventName] [nvarchar](50) NOT NULL,
					[EventData] [nvarchar](max) NOT NULL,
					[Date] [datetime] NOT NULL,
				 CONSTRAINT [PK_EQEvent] PRIMARY KEY CLUSTERED 
				(
					[ID] ASC
				)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
				) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
				END

				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EQSubscriber]') AND type in (N'U'))
				BEGIN
				CREATE TABLE [dbo].[EQSubscriber](
					[ID] [int] IDENTITY(1,1) NOT NULL,
					[SubscriberID] [nvarchar](50) NOT NULL,
					[ActiveSubscriptions] [nvarchar](4000) NOT NULL,
					[LastSeenOnline] [datetime] NOT NULL,
				 CONSTRAINT [PK_EQSubscriber] PRIMARY KEY CLUSTERED 
				(
					[ID] ASC
				)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
				) ON [PRIMARY]
				END

				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EQTracking]') AND type in (N'U'))
				BEGIN
				CREATE TABLE [dbo].[EQTracking](
					[SubscriberID] [int] NOT NULL,
					[EventID] [int] NOT NULL,
					[EventSent] [bit] NOT NULL,
				 CONSTRAINT [PK_EQTracking] PRIMARY KEY CLUSTERED 
				(
					[SubscriberID] ASC,
					[EventID] ASC
				)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
				) ON [PRIMARY]
				END

				IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EQTracking]') AND name = N'DelayedUntil')
				BEGIN
					ALTER TABLE [EQTracking] add [DelayedUntil] DateTime null
				END

				IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EQTracking]') AND name = N'RetryCount')
				BEGIN
					ALTER TABLE [EQTracking] add [RetryCount] int constraint Default_RetryCount_Val default 0
				END

				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EQService]') AND type in (N'U'))
				BEGIN
					CREATE TABLE [dbo].[EQService](
						[ID] [int] IDENTITY(1,1) NOT NULL,
						[Name] [nvarchar](100) NOT NULL,
						CONSTRAINT [PK_EQService] PRIMARY KEY CLUSTERED 
						(
							[ID] ASC
						) ON [PRIMARY]
					) ON [PRIMARY]

					ALTER TABLE [EQEvent] add [ServiceID] int NULL
					ALTER TABLE [EQSubscriber] add [ServiceID] int NULL

					ALTER TABLE [dbo].[EQEvent]  WITH CHECK ADD  CONSTRAINT [FK_EQEvent_EQService] FOREIGN KEY([ServiceID])
					REFERENCES [dbo].[EQService] ([ID])
					ON DELETE CASCADE

					ALTER TABLE [dbo].[EQEvent] CHECK CONSTRAINT [FK_EQEvent_EQService]

					ALTER TABLE [dbo].[EQSubscriber]  WITH CHECK ADD  CONSTRAINT [FK_EQSubscriber_EQService] FOREIGN KEY([ServiceID])
					REFERENCES [dbo].[EQService] ([ID])
					ON DELETE CASCADE

					ALTER TABLE [dbo].[EQSubscriber] CHECK CONSTRAINT [FK_EQSubscriber_EQService]

					ALTER TABLE [dbo].[EQTracking]  WITH CHECK ADD  CONSTRAINT [FK_EQTracking_EQEvent] FOREIGN KEY([EventID])
					REFERENCES [dbo].[EQEvent] ([ID])

					ALTER TABLE [dbo].[EQTracking] CHECK CONSTRAINT [FK_EQTracking_EQEvent]

					ALTER TABLE [dbo].[EQTracking]  WITH CHECK ADD  CONSTRAINT [FK_EQTracking_EQSubscriber] FOREIGN KEY([SubscriberID])
					REFERENCES [dbo].[EQSubscriber] ([ID])
					ON DELETE CASCADE

					ALTER TABLE [dbo].[EQTracking] CHECK CONSTRAINT [FK_EQTracking_EQSubscriber]
				END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EQSubscriber]') AND name = N'RetentionThreshold')
				BEGIN
					ALTER TABLE [EQSubscriber] add [RetentionThreshold] int null
				END
			");
        }
    }
}
