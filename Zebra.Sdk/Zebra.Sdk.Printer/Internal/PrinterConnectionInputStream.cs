using System;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Printer.Internal
{
	internal class PrinterConnectionInputStream : PrinterConnectionInputStreamBase
	{
		public PrinterConnectionInputStream(Connection printerConnection, long maxTimeToWaitForMoreData, string terminator) : base(printerConnection, maxTimeToWaitForMoreData)
		{
			this.terminator = terminator;
		}

		protected override void SetTerminatorBasedOnData(int b)
		{
		}
	}
}