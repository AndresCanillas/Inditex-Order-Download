using System;
using System.IO;

namespace Zebra.Sdk.Printer.Internal
{
	internal abstract class ZebraFileConnection
	{
		public abstract int FileSize
		{
			get;
		}

		protected ZebraFileConnection()
		{
		}

		public abstract void Close();

		public abstract Stream OpenInputStream();
	}
}