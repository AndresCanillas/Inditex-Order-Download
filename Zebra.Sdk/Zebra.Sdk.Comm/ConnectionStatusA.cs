using System;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       Abstract class which implements the default functionality of <c>StatusConnection</c>.
	///       </summary>
	public abstract class ConnectionStatusA : ConnectionA, StatusConnectionWithWriteLogging, StatusConnection, Connection
	{
		protected ConnectionStatusA()
		{
		}
	}
}