using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public interface IMsgPeer : IDisposable
	{
		event EventHandler OnConnect;
		event EventHandler OnDisconnect;
		event EventHandler<IMsgSession> OnSessionReady;

		// API when acting as a server

		int ListenerPort { get; }

		bool IsStarted { get; }

		/// <summary>
		/// Starts listening for connections on the specified port. NOTE: Make sure you run the program as administrator, or run net start
		/// </summary>
		/// <param name="port"></param>
		void Start(int port);

		/// <summary>
		/// Stops listening for connections and also closes all active sessions.
		/// </summary>
		void Stop();


		// API when acting as a client

		/// <summary>
		/// Remote end point to which we are connected. NOTE: returned value will be null if Connect has not been called at least once.
		/// </summary>
		IPEndPoint EndPoint { get; }

		/// <summary>
		/// Flag indicating if we are currently connected to the remote end point.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Attempts to start connection with the remote end point
		/// </summary>
		/// <param name="ep">Remote end point to which we want to connect.</param>
		void Connect(IPEndPoint ep);

		/// <summary>
		/// Connects to the specified server and port (server might be a machine name, DNS name or IPAddress).
		/// NOTE: If you know the IPAddress of the target machine, use Connect(IPEndPoint) to avoid a DNS lookup and make connection faster.
		/// </summary>
		/// <param name="server">Server name, dns name or IP Address to which we want to connect to.</param>
		/// <param name="port">TCP port used to connect to the server</param>
		void Connect(string server, int port);

		/// <summary>
		/// Closes the connection with the server.
		/// </summary>
		void Disconnect();

		// The following API definitions are available regardless of role (can be called while working as either server or client)

		/// <summary>
		/// Registers a service that can be invoked by the system on the other end of the connection.
		/// </summary>
		/// <typeparam name="T">The service being registered so that remote system can make calls to it. T must be an interface and must be registered as a singleton in IFactory, otherwise an exception will be thrown.</typeparam>
		/// <remarks>T must exclusively define methods and/or events, and the types used to declare those methods and events are restricted to those supported by the Messaging system, rule of thumb: keep things simple and you should be fine.</remarks>
		void PublishService<T>() where T : class;

		/// <summary>
		/// Retreives a proxy to a remote service that can be used to invoke remote code.
		/// </summary>
		/// <typeparam name="T">The service to be retrieved.</typeparam>
		T GetServiceProxy<T>() where T : class;
        void SimulateClientReceiveInvalidMessage();
    }


	public interface IMsgSession : IDisposable
	{
		IScope Scope { get; }
		bool IsConnected { get; }
		IPEndPoint EndPoint { get; }

		void SendRequest(RequestInfo rq);
		void SendMessage(RequestInfo rq);

		void ClientSideRegisterEvent(int contractid, int eventid);
		void ClientSideUnregisterEvent(int contractid, int eventid);
		void StartStreamTransfer(Guid guid);

		T GetService<T>() where T : class;
		void RegisterService<T>(T serviceInstance) where T : class;
		T GetServiceProxy<T>() where T : class;
	}


	public class EventSubscriptionInfo
	{
		public int ContractID;
		public int EventID;

		public EventSubscriptionInfo() { }

		public EventSubscriptionInfo(int contractid, int eventid)
		{
			ContractID = contractid;
			EventID = eventid;
		}
	}


	[Serializable]
	public class MsgException : Exception
	{
		public string OriginalType;
		public string OriginalMessage;
		public string OriginalStackTrace;

		public MsgException(string message, string originalType, string originalMessage, string originalStackTrace)
			: base(message)
		{
			OriginalType = originalType;
			OriginalMessage = originalMessage;
			OriginalStackTrace = originalStackTrace;
		}

        protected MsgException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}


	public class EventData : IDisposable
	{
		public ProtocolBuffer buffer;
		public int contractid;
		public int eventid;

		public EventData(ProtocolBuffer buffer, int contractid, int eventid)
		{
			this.buffer = buffer;
			this.contractid = contractid;
			this.eventid = eventid;
		}

		public void Dispose()
		{
			if (buffer != null)
			{
				buffer.Release();
				buffer = null;
			}
		}
	}
}
