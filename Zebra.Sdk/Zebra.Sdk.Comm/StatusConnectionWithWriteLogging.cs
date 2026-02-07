using System;
using System.IO;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       A status connection to a Link-OS printer. The status connection requires Link-OS firmware 2.5 or higher. This 
	///       connection will not block the printing channel, nor can it print.It copies data sent to the connection to the 
	///       provided stream.
	///       </summary>
	public interface StatusConnectionWithWriteLogging : StatusConnection, Connection
	{
		/// <summary>
		///       Sets the stream to log the write data to.
		///       </summary>
		/// <param name="logStream">Log all write data to this stream.</param>
		void AddWriteLogStream(BinaryWriter logStream);
	}
}