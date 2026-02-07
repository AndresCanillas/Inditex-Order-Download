//using Lextm.SharpSnmpLib;
//using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Zebra.Sdk.Comm.Snmp.Internal
{
	internal abstract class SnmpV1
	{
		public static int port;

		private int timeout;

		protected int maxRetries;

		private string host;

		private string communityName;

		private string oid;

		private bool pduInFlight;

		//protected ISnmpMessage message;

		//protected static VersionCode versionCode;

		public string CommunityName
		{
			get
			{
				return this.communityName;
			}
		}

		public string Host
		{
			get
			{
				return this.host;
			}
		}

		public string Oid
		{
			get
			{
				return this.oid;
			}
			set
			{
				this.oid = value;
			}
		}

		public bool PduInFlight
		{
			get
			{
				return this.pduInFlight;
			}
			set
			{
				this.pduInFlight = value;
			}
		}

		public int Timeout
		{
			set
			{
				this.timeout = value;
			}
		}

		static SnmpV1()
		{
			SnmpV1.port = 161;
			//SnmpV1.versionCode = VersionCode.V1;
		}

		public SnmpV1(string host) : this(host, "public", 5)
		{
		}

		public SnmpV1(string host, string communityName, int maxRetries)
		{
			this.host = host;
			this.communityName = communityName;
			this.maxRetries = maxRetries;
		}

		//public virtual ISnmpPdu GetPdu()
		//{
		//	Task<ISnmpMessage> responseAsync = SnmpMessageExtension.GetResponseAsync(this.message, new IPEndPoint(IPAddress.Parse(this.Host), SnmpV1.port));
		//	if (!responseAsync.Wait(this.timeout))
		//	{
		//		throw new SnmpTimeoutException();
		//	}
		//	if (responseAsync.Status != TaskStatus.RanToCompletion)
		//	{
		//		throw responseAsync.Exception;
		//	}
		//	return responseAsync.Result.Pdu();
		//}

		public abstract void SendRequest();
	}
}