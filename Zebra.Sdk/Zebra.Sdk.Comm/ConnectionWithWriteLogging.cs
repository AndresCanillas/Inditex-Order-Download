using System;
using System.IO;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       A connection to a device that copies data sent to the connection to the provided stream.
	///       </summary>
	public interface ConnectionWithWriteLogging : Connection
	{
		/// <summary>
		///       Sets the stream to log the write data to.
		///       </summary>
		/// <param name="logStream">Log all write data to this stream.</param>
		void AddWriteLogStream(BinaryWriter logStream);
	}
}