//using Lextm.SharpSnmpLib;
//using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Comm.Snmp.Internal
{
	internal class SnmpGet : SnmpV1
	{
		private int timeout;

		public SnmpGet(string host, SnmpPreferences snmpPrefs) : base(host, snmpPrefs.CommunityNameGet, snmpPrefs.MaxRetries)
		{
			this.timeout = snmpPrefs.TimeoutGet;
		}

		public void Init(string oid)
		{
			base.PduInFlight = false;
			base.Oid = oid;
			base.Timeout = this.timeout;
		}

		public override void SendRequest()
		{
			if (!base.PduInFlight)
			{
				base.PduInFlight = true;
				//this.message = new GetRequestMessage(0, SnmpV1.versionCode, new OctetString(base.CommunityName), new List<Variable>()
				//{
				//	new Variable(new ObjectIdentifier(base.Oid))
				//});
				//if (this.message.Pdu().ErrorStatus.ToInt32() != 0)
				//{
				//	base.PduInFlight = false;
				//}
			}
		}
	}
}