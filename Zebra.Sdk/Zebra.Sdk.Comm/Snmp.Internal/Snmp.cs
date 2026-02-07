//using Lextm.SharpSnmpLib;
using System;
using System.Collections.Generic;
using Zebra.Sdk.Device;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Comm.Snmp.Internal
{
	internal class Snmp
	{
		private SnmpPreferences snmpPreferences;

		private SettingType type;

		public Snmp(string snmpGetCommunityName, string snmpSetCommunityName, SettingType type)
		{
			this.snmpPreferences = new SnmpPreferences()
			{
				CommunityNameGet = snmpGetCommunityName,
				CommunityNameSet = snmpSetCommunityName
			};
			this.type = type;
		}

		public virtual string Get(string hostAddress, string oid)
		{
			SnmpGet snmpGet = new SnmpGet(hostAddress, this.snmpPreferences);
			snmpGet.Init(oid);
			snmpGet.SendRequest();
			//return snmpGet.GetPdu().Variables[0].Data.ToString();
			throw new NotImplementedException();
		}

		public virtual void Set(string hostAddress, string oid, string valueToSet)
		{
			SnmpSet snmpSet = new SnmpSet(hostAddress, this.snmpPreferences);
			if (this.type != SettingType.STRING)
			{
				if (this.type != SettingType.ENUM && this.type != SettingType.INTEGER)
				{
					throw new ZebraIllegalArgumentException("Invalid setting type");
				}
				//snmpSet.Init(oid, new Integer32(int.Parse(valueToSet)));
			}
			else
			{
				//snmpSet.Init(oid, new OctetString(valueToSet));
			}
			snmpSet.SendRequest();
			//ErrorCode errorCode = snmpSet.GetPdu().ErrorStatus.ToErrorCode();
			//if (errorCode != ErrorCode.NoError)
			//{
			//	throw new SnmpException(string.Concat("Snmp error: ", errorCode));
			//}
		}
	}
}