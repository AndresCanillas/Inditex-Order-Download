using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Comm.Internal
{
	internal class ConnectionAttributeProvider
	{
		private static Dictionary<Connection, ConnectionAttributes> connectionAttributes;

		static ConnectionAttributeProvider()
		{
			ConnectionAttributeProvider.connectionAttributes = new Dictionary<Connection, ConnectionAttributes>();
		}

		public ConnectionAttributeProvider()
		{
		}

		public ConnectionAttributes GetAttributes(Connection connection)
		{
			if (!ConnectionAttributeProvider.connectionAttributes.ContainsKey(connection))
			{
				ConnectionAttributes connectionAttribute = new ConnectionAttributes();
				ConnectionAttributeProvider.connectionAttributes.Add(connection, connectionAttribute);
			}
			return ConnectionAttributeProvider.connectionAttributes[connection];
		}
	}
}