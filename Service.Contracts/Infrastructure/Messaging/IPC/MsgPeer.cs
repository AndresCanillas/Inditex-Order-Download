using Service.Contracts.IPC;
using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public class MsgPeer : IMsgPeer
	{
		private object syncObj = new object();
		private bool started;
		private bool disposed;
		private EventHandler onconnect;
		private EventHandler ondisconnect;
		private EventHandler<IMsgSession> onsessionready;
		private Socket listener;
		private Timer listenerRestartTimer;
		private List<MsgSession> connections = new List<MsgSession>();
		private MsgSession clientSession;

		private ConcurrentDictionary<int, ServerProxy> services = new ConcurrentDictionary<int, ServerProxy>();
		private IFactory factory;
		private ILogService log;
		internal IScope scope;

		public MsgPeer(IFactory factory, ILogService log)
		{
			this.factory = factory;
			this.log = log;
			listenerRestartTimer = new Timer(ServerRestart, null, Timeout.Infinite, Timeout.Infinite);
			this.scope = factory.CreateScope();
		}


		public void Dispose()
		{
			lock (syncObj)
			{
				if (disposed)
					return;

				disposed = true;
				listenerRestartTimer.Dispose();

				Stop();

				if (clientSession != null)
				{
					clientSession.Disconnect();
					clientSession.OnEnd -= ClientSession_OnEnd;
					clientSession.Dispose();
					clientSession = null;
				}

				onconnect = null;
				ondisconnect = null;
				onsessionready = null;

				services.Clear();
				connections.Clear();
			}
		}


		/// <summary>
		/// Raised when this peer actively connects to a remote end point as result of a sucessful call to Connect.
		/// </summary>
		public event EventHandler OnConnect
		{
			add
			{
				lock (syncObj)
				{
					if (disposed)
						throw new ObjectDisposedException($"This isntance of {nameof(MsgPeer)} has been disposed, cannot set {nameof(OnConnect)} handler while in the disposed state.");
					onconnect += value;
				}
			}
			remove
			{
				lock (syncObj)
				{
					if (disposed)
						return;
					onconnect -= value;
				}
			}
		}


		/// <summary>
		/// Raised when the connection opened with Connect is closed either because Disconnect or Dispose was called, or if the remote
		/// end point closes the connection for any reason. Notice that there are many reasons why the connection might become closed,
		/// some examples include: Transient networking issues, the remote process was terminated, the remote process closed the 
		/// connection, etc. There is no way to know what caused the connection to drop, only that it was droped.
		/// </summary>
		public event EventHandler OnDisconnect
		{
			add
			{ 
				lock (syncObj)
				{
					if (disposed)
						throw new ObjectDisposedException($"This isntance of {nameof(MsgPeer)} has been disposed, cannot set {nameof(OnDisconnect)} handler while in the disposed state.");
					ondisconnect += value;
				}
			}
			remove
			{
				lock (syncObj)
				{
					if (disposed)
						return;
					ondisconnect -= value;
				}
			}
		}


		/// <summary>
		/// Raised when a new session has been accepted by the peer
		/// </summary>
		public event EventHandler<IMsgSession> OnSessionReady
		{
			add
			{
				lock (syncObj)
				{
					if (disposed)
						throw new ObjectDisposedException($"This isntance of {nameof(MsgPeer)} has been disposed, cannot set {nameof(OnSessionReady)} handler while in the disposed state.");
					onsessionready += value;
				}
			}
			remove
			{
				lock (syncObj)
				{
					if (disposed)
						return;
					onsessionready -= value;
				}
			}
		}


		private void RaiseOnSessionReady(object st)
		{
			try
			{
				var session = st as MsgSession;
				EventHandler<IMsgSession> dlg;  // snapshot delegate, but invoke outside of lock
				lock (syncObj)
				{
					if (disposed)
						return;
					dlg = onsessionready;
				}
				dlg?.Invoke(this, session);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		private void RaiseOnConnect(object state)
		{
			try
			{
				EventHandler dlg;  // snapshot delegate, but invoke outside of lock
				lock (syncObj)
				{
					if (disposed)
						return;
					dlg = onconnect;
				}
				dlg?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		private void RaiseOnDisconnect(object state)
		{
			try
			{
				EventHandler dlg;  // snapshot delegate, but invoke outside of lock
				lock (syncObj)
				{
					if (disposed)
						return;
					dlg = ondisconnect;
				}
				dlg?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		public void PublishService<T>() where T : class
		{
			T instance = factory.GetInstance<T>();
			RegisterService<T>(instance);
		}


		public void RegisterService<T>(T instance) where T : class
		{
			lock (syncObj)
			{
				if (disposed)
					throw new ObjectDisposedException($"This {nameof(MsgPeer)} has been disposed, cannot invoke {nameof(PublishService)} on a disposed object.");
			}

			ServerProxy srv;
			Type t = typeof(T);

			if (!t.IsInterface)
				throw new InvalidOperationException("Service Type must be an interface type");

			ServiceLifeTime lifetime = factory.GetServiceLifeTime<T>();
			if (lifetime != ServiceLifeTime.Singleton)
				throw new InvalidOperationException("Service must be registered as a singleton within the DI container");

			int serviceid = JenkinsHash.Compute(t.Name);
			if (services.TryGetValue(serviceid, out srv))
				throw new InvalidOperationException($"Service {t.Name} has already been registered within this MsgPeer instance");

			srv = DynamicCodeGen.CreateServerProxy(t, this, log, instance);
			if (!services.TryAdd(serviceid, srv))  // This prevents possible race condition by validating registration again
				throw new InvalidOperationException($"Service {t.Name} has already been registered within this MsgPeer instance");

			srv.Start();
		}


		public T GetService<T>() where T : class
		{
			lock (syncObj)
			{
				if (disposed)
					throw new ObjectDisposedException($"This {nameof(MsgPeer)} has been disposed, cannot invoke {nameof(GetService)} on a disposed object.");
			}

			ServerProxy srv;
			Type t = typeof(T);
			int serviceid = JenkinsHash.Compute(t.Name);
			if (!services.TryGetValue(serviceid, out srv))
				throw new Exception("Requested service is not registered.");

			return srv.ServiceInterface as T;
		}


		internal ServerProxy GetService(int contractid)
		{
			lock (syncObj)
			{
				if (disposed)
					throw new ObjectDisposedException($"This {nameof(MsgPeer)} has been disposed, cannot invoke {nameof(GetService)} on a disposed object.");
			}

			ServerProxy service;
			services.TryGetValue(contractid, out service);
			if (service == null)
				throw new Exception("Requested service is not registered.");

			return service;
		}


		public IEnumerable<string> RegisteredServices
		{
			get
			{
				List<string> result = new List<string>();
				foreach (ServerProxy srv in services.Values)
				{
					result.Add(srv.ContractName);
				}
				return result;
			}
		}


		private List<MsgSession> GetActiveSessions()
		{
			lock (syncObj)
			{
				var snapshot = new List<MsgSession>(connections);
				return snapshot;
			}
		}


		internal void SendEvent(EventData evt)
		{
			using (evt)
			{
				List<MsgSession> activeConnections = null;
				lock (syncObj)
				{
					if (disposed)
						return;

					activeConnections = new List<MsgSession>(connections);
				}

				foreach (MsgSession session in activeConnections)
				{
					try
					{
						if (session.IsSubscriber(evt.contractid, evt.eventid))
							session.SendMessage(new RequestInfo(session, evt));
					}
					catch { }  // Ignore errors and continue sending event to the remaining sessions
				}
			}
		}



		//============================================================================
		// Server Side API
		//============================================================================
		#region Server Side API
		
		public int ListenerPort
		{
			get;
			internal set;
		}


		public bool IsStarted
		{
			get
			{
				lock (syncObj)
				{
					return started;
				}
			}
		}


		public void Start(int port)
		{
			lock (syncObj)
			{
				if (disposed)
					throw new ObjectDisposedException($"This {nameof(MsgPeer)} has been disposed, cannot invoke {nameof(Start)} on a disposed object.");

				if (started)
					throw new Exception($"Listener is already started on port {ListenerPort}");

				try
				{
					ListenerPort = port;
					listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

					started = true;

					listener.Bind(new IPEndPoint(IPAddress.Any, port));
					listener.Listen(50);
					listener.BeginAccept(ConnectionReady, null);
				}
				catch
				{
					Stop();
					throw;
				}
			}
		}


		public void Stop()
		{
			var activeSessions = StopListener();

			if (activeSessions != null)
			{
				foreach (MsgSession session in activeSessions)
				{
					try
					{
						session.Dispose();
					}
					catch (Exception ex)
					{
						log.LogException(ex);
					}
				}
			}
		}


		private IEnumerable<MsgSession> StopListener()
		{
			lock (syncObj)
			{
				if (!started)
					return null;

				started = false;
				if (listener != null)
				{
					listener.Dispose();
					listener = null;
				}

				return GetActiveSessions();
			}
		}


		private void ConnectionReady(IAsyncResult ar)
		{
			Socket s = null;
			lock (syncObj)
			{
				if (!started || disposed)
					return;

				try
				{
					s = listener.EndAccept(ar);
					listener.BeginAccept(ConnectionReady, null);
				}
				catch (Exception ex)
				{
					if (s != null)
					{
                        try { s.Dispose(); }
                        catch { } // Intentional empty catch, we are already handling a fault and heading out, so any error here can be safely ignored.
					}

					log.LogException("Error while accepting new connection, listener will be restarted.", ex);
					listenerRestartTimer.Change(100, Timeout.Infinite);
				}

				MsgSession session = new MsgSession(factory.CreateScope(), this, s, ServerSession_OnEnd);
				connections.Add(session);
				log.LogMessage($"Accepted connection from {s.RemoteEndPoint.ToString()}");
				ThreadPool.QueueUserWorkItem(RaiseOnSessionReady, session);
			}
		}


		private void ServerRestart(object state)
		{
			lock (syncObj)
			{
				try
				{
					if (started || disposed)
						return;

					StopListener();
					Start(ListenerPort);
				}
				catch(Exception ex)
				{
					log.LogException("Error while trying to restart Listener, will retry later.", ex);
					listenerRestartTimer.Change(100, Timeout.Infinite);
				}
			}
		}


		private void ServerSession_OnEnd(object sender, EventArgs e)
		{
			try
			{
				MsgSession session = sender as MsgSession;
				session.OnEnd -= ServerSession_OnEnd;
				lock (syncObj)
				{
					connections.Remove(session);
				}
				session.Dispose();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}

		#endregion


		//============================================================================
		// Client Side API
		//============================================================================
		#region Client Side API
		
		public void Connect(string server, int port)
		{
			var ep = TcpHelper.GetEndPoint(server, port);
			Connect(ep);
		}


		public void Connect(IPEndPoint endPoint)
		{
			lock (syncObj)
			{
				if (disposed)
					throw new ObjectDisposedException($"This {nameof(MsgPeer)} was disposed, cannot invoke {nameof(Connect)} on a disposed object.");

				EnsureClientSessionCreated();
				if (clientSession.IsConnected)
					throw new Exception($"Already connected to end point.");

				try
				{
					clientSession.Connect(endPoint);
					connections.Add(clientSession);
					ThreadPool.QueueUserWorkItem(RaiseOnConnect, null);
				}
				catch (Exception ex)
				{
					if (clientSession.IsConnected)
					{
						log.LogException("Client connection will be closed due to an error", ex);
						clientSession.Disconnect();
						connections.Remove(clientSession);
					}
					throw;
				}
			}
		}


		private void ClientSession_OnEnd(object sender, EventArgs e)
		{
			try
			{
				lock (syncObj)
				{
					if (disposed)
						return;

					connections.Remove(clientSession);
					ThreadPool.QueueUserWorkItem(RaiseOnDisconnect, null);
				}
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		public void Disconnect()
		{
			lock (syncObj)
			{
				if (disposed || clientSession == null)
					return;

				clientSession.Disconnect();
			}
		}


		public IPEndPoint EndPoint
		{
			get
			{
				lock (syncObj)
				{
					EnsureClientSessionCreated();
					return clientSession.EndPoint;
				}
			}
		}


		public bool IsConnected
		{
			get
			{
				lock (syncObj)
				{
					EnsureClientSessionCreated();
					return clientSession.IsConnected;
				}
			}
		}


		public T GetServiceProxy<T>() where T : class
		{
			lock (syncObj)
			{
				if (disposed)
					throw new ObjectDisposedException($"This {nameof(MsgPeer)} was disposed, cannot invoke {nameof(GetServiceProxy)} on a disposed object.");
				EnsureClientSessionCreated();
				return clientSession.GetServiceProxy<T>();
			}
			throw new InvalidOperationException($"Cannot invoke {nameof(GetServiceProxy)} while disconnected.");
		}

		private void EnsureClientSessionCreated()
		{
			if (clientSession == null)
			{
				clientSession = new MsgSession(factory.CreateScope(), this);
				clientSession.OnEnd += ClientSession_OnEnd; // Note: This subscription is removed inside Dispose
			}
		}

		internal object GetServiceProxy(Type serviceContract)
		{
			lock (syncObj)
			{
				if (disposed)
					throw new ObjectDisposedException($"This {nameof(MsgPeer)} was disposed, cannot invoke {nameof(GetServiceProxy)} on a disposed object.");
				EnsureClientSessionCreated();
				return clientSession.GetServiceProxy(serviceContract);
			}
		}

        public void SimulateClientReceiveInvalidMessage()
        {
            clientSession.SimulateClientReceiveInvalidMessage();
        }

        #endregion
    }
}
