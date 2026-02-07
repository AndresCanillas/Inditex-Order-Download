using System;
using Zebra.Sdk.Comm.Internal;

namespace Zebra.Sdk.Comm.Snmp.Internal
{
	internal class SnmpPreferences
	{
		private static int MAXRETRIES;

		private static int TIMEOUTGET;

		private static int TIMEOUTSET;

		private string communityNameGet = "public";

		private string communityNameSet = "public";

		private int maxRetries = SnmpPreferences.MAXRETRIES;

		private int timeoutGet = SnmpPreferences.TIMEOUTGET;

		private int timeoutSet = SnmpPreferences.TIMEOUTSET;

		public string CommunityNameGet
		{
			get
			{
				return this.communityNameGet;
			}
			set
			{
				this.communityNameGet = value;
			}
		}

		public string CommunityNameSet
		{
			get
			{
				return this.communityNameSet;
			}
			set
			{
				this.communityNameSet = value;
			}
		}

		public int MaxRetries
		{
			get
			{
				return this.maxRetries;
			}
		}

		public int TimeoutGet
		{
			get
			{
				return this.timeoutGet;
			}
		}

		public int TimeoutSet
		{
			get
			{
				return this.timeoutSet;
			}
		}

		static SnmpPreferences()
		{
			SnmpPreferences.MAXRETRIES = 4;
			SnmpPreferences.TIMEOUTGET = 5000;
			SnmpPreferences.TIMEOUTSET = 5000;
		}

		public SnmpPreferences()
		{
		}

		public SnmpPreferences(string communityNameGet, string communityNameSet)
		{
			this.communityNameGet = communityNameGet;
			this.communityNameSet = communityNameSet;
		}

		public SnmpPreferences(ConnectionAttributes connAttributes)
		{
			this.communityNameGet = connAttributes.snmpGetCommunityName;
			this.communityNameSet = connAttributes.snmpSetCommunityName;
			this.timeoutGet = connAttributes.snmpTimeoutGet;
			this.timeoutSet = connAttributes.snmpTimeoutSet;
			this.maxRetries = connAttributes.snmpMaxRetries;
		}
	}
}