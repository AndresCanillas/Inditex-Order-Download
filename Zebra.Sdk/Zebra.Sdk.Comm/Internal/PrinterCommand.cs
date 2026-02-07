using System;
using System.IO;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Comm.Internal
{
	internal interface PrinterCommand
	{
		byte[] SendAndWaitForResponse(Connection printerConnection);

		byte[] SendAndWaitForResponse(Connection printerConnection, int maxTimeoutForRead, int timeToWaitForMoreData);

		byte[] SendAndWaitForResponse(Connection printerConnection, int maxTimeoutForRead, int timeToWaitForMoreData, string terminator);

		void SendAndWaitForResponse(BinaryWriter response, Connection printerConnection);

		void SendAndWaitForResponse(BinaryWriter response, Connection printerConnection, int maxTimeoutForRead, int timeToWaitForMoreData, string terminator);
	}
}