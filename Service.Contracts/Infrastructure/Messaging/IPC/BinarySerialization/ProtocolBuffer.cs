using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Service.Contracts
{
	/* 
	 * Extends SerializationBuffer to create messages that can be sent over the network to communicate with a remote process by:
	 * 
	 *   > Calling remote services
	 *   > Receive responses to those calls
	 *   > Subscribe to events
	 *   > Receive event notifications
	 *   > Send/Receive files using Streams
	 * 
	 */

	/// <summary>
	/// An enumeration containing all the opcodes defined in the protocol.
	/// </summary>
	public enum MsgOpcode
	{
		None = 0x00,
		EventSubscription = 0x1E,   // This opcode allows registering the events that are of interest to the remote end point. IMPORTANT: By default events are not transmitted to a remote host unless the remote host has actively subscribed to them. Also, at the present time, delivery of events is done only while connected, and there is no way to transmit events that occurred while the other peer was disconnected or not actively subscribed.
		HeartBeat = 0x1F,           // Used to verify the health of the connection. This opcode is sent by the peers after N seconds of innactivity to ensure the connection is healthty.
		Invoke = 0x20,              // Used to invoke a synchronous method defined on a service published through the MsgPeer class.
		InvokeAsync = 0x21,			// Used to invoke an asynchronous method that follows the Task (async/await) signature.
		Response = 0x22,            // Used to send the result of a method invocation back to the caller (all 3 variants of Invoke use this OpCode to return results back to the caller).
		Exception = 0x23,           // Used to let the remote endpoint know that a method invocation generated an exception (all 3 variants of Invoke use this OpCode to return errors back to the caller).
		Event = 0x24,               // Used to notify an event to the remote end point.
		StreamRequest = 0x25,		// Used to start the transfer of a previously registered Stream.
		StreamBlock = 0x26			// Used to represent a block of data pertaining to a Stream.
	}


	public class ProtocolBuffer : SerializationBuffer
	{
		// ProtocolBuffer operation parameters
		// Can be freely changed to meet specific system needs, they could even be configuration options in appsettings.json; 
		// however these defaults work fine for me, so im not making them configurable.

		private const int MAX_BUFFER_SIZE = 197263360;    //180MB	<--- Maximum amount of data that can be received in a single message, if you reach this point you should consider: Breaking down the API calls (for instance get data in pages instead of all records at once), or using a stream to transfer large amounts of data.
		private const int SHRINK_SIZE_THRESHOLD = 65536;  //64KB	<--- Threshold after wich the buffer is going to be shrink, shrinking happens after N minutes since the last time the buffer was required to grow past this size.
		private const int SHRINK_TIME_THRESHOLD = 1;      //1min	<--- Determines how log before the buffer is shrinked back to small buffer size (asuming the buffer size is greather than SHRINK_SIZE_THRESHOLD).

		private bool headerInitialized;     // Flag indicating if the header information of the message at the beggining of the buffer has been loaded successfully
		internal int msgLength;             // Length of the message at the beggining of the buffer (meaningull only if headerInitialized is true)
		internal int msgid;                 // ID of the message at the beggining of the buffer (meaningull only if headerInitialized is true)
		internal MsgOpcode opcode;          // Opcode of the message at the beggining of the buffer (meaningull only if headerInitialized is true)
		internal bool msgCompleted = true;  // Flag used to validate we dont start a new message before ending a previous one and viceversa

		private int msgIndex;               // Index of the starting point of the last message added to the buffer
		//private IBufferManager bufferManager;

		internal ProtocolBuffer(IScope scope)
			: base(scope, null, 0, 0)
		{
			//bufferManager = scope.GetInstance<IBufferManager>();
			buffer = new byte[10000];//bufferManager.AcquireSmallBuffer();
		}


		internal ProtocolBuffer(IScope scope, int size)
			: base(scope, null, 0, 0)
		{
			//bufferManager = scope.GetInstance<IBufferManager>();
			buffer = new byte[size];//bufferManager.AcquireBuffer(size);
		}


		public override void EnsureCapacity(int requiredBufferSize)
		{
			if (requiredBufferSize > buffer.Length)
			{
				byte[] tmp = new byte[requiredBufferSize * 2]; //bufferManager.AcquireBuffer(requiredBufferSize + 1000);
				Buffer.BlockCopy(buffer, 0, tmp, 0, availableData);
				//bufferManager.ReleaseBuffer(buffer);
				buffer = tmp;
				lastResize = DateTime.Now;
			}
		}


		public void Release()
		{
			if (buffer != null)
			{
				//bufferManager.ReleaseBuffer(buffer);
				buffer = null;
			}
		}


		// Extracts a copy of the message at the start of the buffer and removes the extracted message from the buffer.
		// If there are other messages in the buffer (including incomplete messages), they are shifted to the left so that
		// they sit at the begginging of the buffer.

		internal bool TryExtractMessage(out ProtocolBuffer message)
		{
			message = null;

			if (availableData > 16)
			{
				if (!headerInitialized)
					InitHeader();

				if (availableData >= msgLength)
				{
					message = MakeCopy();
					message.position = 16;

					DiscardMessage(); // This removes the copied message from the buffer

					if (availableData == 0 &&
						buffer.Length > SHRINK_SIZE_THRESHOLD &&
						lastResize.AddMinutes(1) < DateTime.Now)
					{
						ShrinkBuffer();
					}
					return true;
				}
				else if (buffer.Length < msgLength)
				{
					EnsureCapacity(msgLength + 500);
				}
			}
			return false;
		}


		private void InitHeader()
		{
			byte headerStart = buffer[0];
			byte opcode = buffer[1];
			int msgLen = PeekInt32(4);
			msgid = PeekInt32(8);
			int headerEnd = PeekInt32(12);
			this.opcode = (MsgOpcode)opcode;
			msgLength = msgLen;

			if (headerStart != 0xAA ||
				headerEnd != 0x5901F781 ||
				msgLen < 16 ||
				msgLen % 16 != 0 ||
				msgLen > MAX_BUFFER_SIZE ||
				opcode < (int)MsgOpcode.EventSubscription ||
				opcode > (int)MsgOpcode.StreamBlock)
			{
				int dumpSize = availableData > 96 ? 96 : availableData;
				throw new Exception($"Received an invalid message... HS: {headerStart.ToString("X2")}, HE: {headerEnd.ToString("X4")}, Opcode: {opcode}, Len: {msgLen}\r\n{buffer.HexDump(0, dumpSize, 16)}");
			}
			else
			{
				headerInitialized = true;
			}
		}


		public void ValidateMessage()
		{
			if (availableData < 16)
				throw new Exception("Message length cannot be smaller than 16 bytes");

			var headerStart = buffer[0];
			var opcode = buffer[1];
			var msgLen = PeekInt32(4);
			var headerEnd = PeekInt32(12);

			if (headerStart != 0xAA ||
				headerEnd != 0x5901F781 ||
				msgLen < 16 ||
				msgLen % 16 != 0 ||
				msgLen > MAX_BUFFER_SIZE ||
				opcode < (int)MsgOpcode.EventSubscription ||
				opcode > (int)MsgOpcode.StreamBlock)
			{
				int dumpSize = availableData > 96 ? 96 : availableData;
				throw new Exception($"Invalid message... HS: {headerStart.ToString("X2")}, HE: {headerEnd.ToString("X4")}, Opcode: {opcode}, Len: {msgLen}\r\n{buffer.HexDump(0, dumpSize, 16)}");
			}
		}


		// Makes a copy of the message at the start of the buffer
		public ProtocolBuffer MakeCopy()
		{
			ProtocolBuffer copy = new ProtocolBuffer(scope, msgLength);
			Array.Copy(buffer, 0, copy.buffer, 0, msgLength);
			copy.position = 0;
			copy.availableData = msgLength;
			copy.headerInitialized = headerInitialized;
			copy.opcode = opcode;
			copy.msgLength = msgLength;
			copy.msgid = msgid;

			return copy;
		}


		private void DiscardMessage()
		{
			if (msgLength > availableData)
				throw new Exception("Invalid argument: MsgLen");

			if (availableData > msgLength)
			{
				int remainingBytes = availableData - msgLength;
				var tmpBuffer = new byte[remainingBytes];
				Array.Copy(buffer, msgLength, tmpBuffer, 0, remainingBytes);
				Array.Copy(tmpBuffer, 0, buffer, 0, remainingBytes);
				position = 0;
				availableData = remainingBytes;
			}
			else
			{
				position = 0;
				availableData = 0;
			}
			headerInitialized = false;
			opcode = MsgOpcode.None;
			msgLength = 0;
			msgid = 0;
		}


		private void ShrinkBuffer()
		{
			//bufferManager.ReleaseBuffer(buffer);
			buffer = new byte[10000];//bufferManager.AcquireSmallBuffer();
			position = 0;
			availableData = 0;
			headerInitialized = false;
			opcode = MsgOpcode.None;
			msgLength = 0;
			msgid = 0;
		}


		public void StartMessage(MsgOpcode opcode, int msgid)
		{
			if (!msgCompleted)
				throw new InvalidOperationException("Must call EndMessage before calling StartMessage again.");

			EnsureCapacity(position + 16);
			this.opcode = opcode;
			msgIndex = position;
			SetInt64(position, 0x00000000000000AAL);
			SetInt64(position + 8, 0x5901F78100000000L);
			SetByte(position + 1, (byte)opcode);
			SetInt32(position + 8, msgid);
			availableData += 16;
			position += 16;
			msgCompleted = false;
		}


		public void EndMessage()
		{
			if (msgCompleted)
				throw new InvalidOperationException("Must call StartMessage before calling EndMessage. Also EndMessage should only be called once per each call to StartMessage.");

			int len;

			if (position % 16 > 0)
				AddPadding();

			len = position - msgIndex;
			SetInt32(msgIndex + 4, len);
			msgCompleted = true;
			msgLength = availableData;
		}


		private void AddPadding()
		{
			int padding = 16 - position % 16;

			EnsureCapacity(position + padding);

			for (var i = 0; i < padding; i++)
				buffer[position + i] = 0;

			position += padding;
			availableData += padding;
		}


		public void SetError(Exception error)
		{
			if (msgIndex != 0)
				throw new InvalidOperationException("SetError can only be used in a buffer that contains a single message.");

			var log = this.scope.GetInstance<ILogService>();
			log.LogException(error);

			SetByte(1, (byte)MsgOpcode.Exception);
			availableData = 16;
			position = 16;
			AddString(error.GetType().Name);
			AddString(error.Message);
			AddString(error.StackTrace);
			EndMessage();
		}
	}
}
