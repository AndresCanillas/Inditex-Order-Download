using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public abstract class ServerProxy
	{
		protected MsgPeer peer;
		protected ILogService log;

		public string ContractName;
		public object ServiceInterface;

		public ServerProxy(MsgPeer peer, ILogService log)
		{
			this.peer = peer;
			this.log = log;
		}

		/// <summary>
		/// Initializes the service. This method is called when the MsgServer is started, it wires to all the service events.
		/// </summary>
		public abstract void Start();

		/// <summary>
		/// Stops the service. This method is called when the MsgServer is about to be stopped, it unwires from the service events.
		/// </summary>
		public abstract void Stop();

		/// <summary>
		/// Executes the specified synchronous method.
		/// </summary>
		public abstract void InvokeMethod(int methodID, RequestInfo rq);

		/// <summary>
		/// Executes the specified async method using the regular Task (async/await) signature.
		/// </summary>
		public abstract Task InvokeMethodAsync(int methodID, RequestInfo rq);


		protected EventData CreateEvent(int contractid, int eventid)
		{
			ProtocolBuffer buffer = new ProtocolBuffer(peer.scope);
			buffer.StartMessage(MsgOpcode.Event, 0);
			buffer.AddInt32(contractid);
			buffer.AddInt32(eventid);
			return new EventData(buffer, contractid, eventid);
		}


		protected void SendEvent(EventData e)
		{
			e.buffer.EndMessage();
			peer.SendEvent(e);
		}
	}
}