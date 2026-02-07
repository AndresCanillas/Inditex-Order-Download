//using Lextm.SharpSnmpLib;
//using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Comm.Snmp.Internal
{
	internal class SnmpSet : SnmpV1
	{
		private int timeout;

		//private ISnmpData @value;

		public SnmpSet(string host) : base(host)
		{
			//this.message = null;
		}

		public SnmpSet(string host, SnmpPreferences snmpPrefs) : base(host, snmpPrefs.CommunityNameSet, snmpPrefs.MaxRetries)
		{
			this.timeout = snmpPrefs.TimeoutSet;
			//this.message = null;
		}

		public void Init(string oid, ISnmpData value)
		{
			throw new NotImplementedException();
			//base.PduInFlight = false;
			//base.Oid = oid;
			//base.Timeout = this.timeout;
			//this.@value = value;
		}

		public override void SendRequest()
		{
			throw new NotImplementedException();
			//if (!base.PduInFlight)
			//{
			//	base.PduInFlight = true;
			//	this.message = new SetRequestMessage(0, SnmpV1.versionCode, new OctetString(base.CommunityName), new List<Variable>()
			//	{
			//		new Variable(new ObjectIdentifier(base.Oid), this.@value)
			//	});
			//	if (this.message.Pdu().ErrorStatus.ToInt32() != 0)
			//	{
			//		base.PduInFlight = false;
			//	}
			//}
		}
	}
}