using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public abstract class ClientProxy : IEquatable<ClientProxy>
	{
		private static int currentid;

		private static int NextID()
		{
			return Interlocked.Increment(ref currentid);
		}

		private int id;
		private int contractid;
		protected IScope scope;
		protected IMsgSession session;

		public IMsgSession Session { get => session; }

		public ClientProxy(IScope scope, int contractid)
		{
			id = NextID();
			this.contractid = contractid;
			this.scope = scope;
			this.session = scope.GetInstance<IMsgSession>();
		}

		public int ID { get { return id; } }

		public int ContractID { get { return contractid; } }

		public bool Connected
		{
			get { return session.IsConnected; }
		}

		public abstract void OnReceiveEvent(int eventid, ProtocolBuffer buffer);

		protected void RegisterEvent(int eventid)
		{
			session.ClientSideRegisterEvent(contractid, eventid);
		}

		protected void UnregisterEvent(int eventid)
		{
			session.ClientSideUnregisterEvent(contractid, eventid);
		}


		protected RequestInfo StartRequest(int methodid, bool asyncCall)
		{
			var rq = new RequestInfo(session, asyncCall);
			rq.output.AddInt32(contractid);
			rq.output.AddInt32(methodid);
			return rq;
		}


		public bool Equals(ClientProxy other)
		{
			if (other == null) return false;
			return other.id == id;
		}
	}
}
