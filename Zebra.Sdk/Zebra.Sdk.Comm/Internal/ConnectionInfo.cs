using System;

namespace Zebra.Sdk.Comm.Internal
{
	public class ConnectionInfo
	{
		private string myData;

		public ConnectionInfo(string inputString)
		{
			this.myData = inputString;
		}

		public string GetMyData()
		{
			return this.myData;
		}
	}
}