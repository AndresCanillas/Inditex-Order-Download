using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public class RemoteStream : Stream
	{
		class BlockInfo
		{
			public ProtocolBuffer buffer;

			public BlockInfo(ProtocolBuffer buffer)
			{
				this.buffer = buffer;
			}

			public int AvailableBytes { get => buffer.availableData - buffer.position; }

			public int Read(byte[] dst, int offset, int count)
			{
				if (AvailableBytes < count)
					throw new Exception("There is not enough available data in this block.");

				Array.Copy(buffer.buffer, buffer.position, dst, offset, count);
				buffer.position += count;
				return count;
			}
		}

		private object syncObj = new object();
		private IMsgSession session;
		private IMsgStreamService streamService;
		private Guid guid;
		private int length;
		private int position;
		private bool activeRead;
		private bool transferInitialized;
		private bool receivedLastBlock;
		private bool transferComplete;
		private int availableBytes;
		private int requestedBytes;
		private Queue<BlockInfo> blocks;
		private ManualResetEvent waitHandle;
		private bool disposed;


		public RemoteStream(IScope scope, Guid guid, int length)
		{
			this.guid = guid;
			this.length = length;
			waitHandle = new ManualResetEvent(false);
			blocks = new Queue<BlockInfo>();
			session = scope.GetInstance<IMsgSession>();
			streamService = scope.GetInstance<IMsgStreamService>();
			streamService.RegisterStream(guid, this);
		}


		public override bool CanRead { get => true; }
		public override bool CanSeek { get => false; }
		public override bool CanWrite { get => false; }
		public override long Length
		{
			get
			{
				if (length < 0)
					throw new NotSupportedException("This operation is not supported by the underlying stream. Instead of relying on the stream length, simply read until you receive 0 bytes; reading 0 bytes from the stream means you have reached the end of the file.");
				else
					return length;
			}
		}


		public override long Position
		{
			get
			{
				return position;
			}
			set
			{
				throw new InvalidOperationException($"{nameof(RemoteStream)} does not support seeking");
			}
		}


		public override void Flush()
		{
		}


		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException($"{nameof(RemoteStream)} does not support calling {nameof(Seek)}");
		}


		public override void SetLength(long value)
		{
			throw new InvalidOperationException($"{nameof(RemoteStream)} does not support calling {nameof(SetLength)}");
		}


		public void HandleStreamBlock(ProtocolBuffer buffer)
		{
			var block = new BlockInfo(buffer);
			lock (syncObj)
			{
				if (disposed)
				{
					buffer.Release();
				}
				else
				{
					blocks.Enqueue(block);
					availableBytes += block.AvailableBytes;
					if (availableBytes >= requestedBytes || block.AvailableBytes == 0)
					{
						if (block.AvailableBytes == 0)
							receivedLastBlock = true;
						waitHandle.Set();
					}
				}
			}
		}


		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count == 0)
				return 0;

			lock (syncObj)
			{
				if (transferComplete)
					return 0;
				ValidateStreamOperation(buffer, offset, count);
			}

			if (!waitHandle.WaitOne(10000))
				throw new Exception("Timed out while waiting for remote end point to send any data on stream");

			return ReadFromBlocks(buffer, offset, count);
		}


		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (count == 0)
				return 0;

			lock (syncObj)
			{
				if (transferComplete)
					return 0;
				ValidateStreamOperation(buffer, offset, count);
			}

			await waitHandle.WaitOneAsync(cancellationToken);
			return ReadFromBlocks(buffer, offset, count);
		}


		private void ValidateStreamOperation(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			if (count < 0)
				throw new InvalidOperationException("count argument cannot be negative");

			if (offset < 0 || offset + count > buffer.Length)
				throw new IndexOutOfRangeException("The combination of offset and count would fall outside the bounds of the supplied buffer.");

			if (activeRead)
				throw new InvalidOperationException("There is another thread reading from this stream, concurrent read operations on the same stream are not supported.");

			if (!transferInitialized)
			{
				session.StartStreamTransfer(guid);
				transferInitialized = true;
			}

			requestedBytes = count;
			activeRead = true;
		}


		private int ReadFromBlocks(byte[] buffer, int offset, int count)
		{
			lock (syncObj)
			{
				if (disposed)
					throw new ObjectDisposedException($"This instance of {nameof(RemoteStream)} was disposed");

				int rb, copiedBytes = 0;
				while (copiedBytes < count && blocks.Count > 0)
				{
					var block = blocks.Peek();
					if(block.AvailableBytes == 0)
					{
						blocks.Dequeue();
						block.buffer.Release();
						transferComplete = true;
						streamService.UnregisterStream(guid);
						break;
					}
					else if (copiedBytes + block.AvailableBytes > count)
					{
						var bytesToRead = count - copiedBytes;
						rb = block.Read(buffer, offset + copiedBytes, bytesToRead);
						copiedBytes += rb;
					}
					else
					{
						rb = block.Read(buffer, offset + copiedBytes, block.AvailableBytes);
						copiedBytes += rb;
						blocks.Dequeue();
						block.buffer.Release();
					}
				}

				position += copiedBytes;
				availableBytes -= copiedBytes;
				if (availableBytes <= 0 && !transferComplete && !receivedLastBlock)
					waitHandle.Reset();
				activeRead = false;
				return copiedBytes;
			}
		}


		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException($"{nameof(RemoteStream)} does not support calling {nameof(Write)}");
		}


		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (syncObj)
				{
					disposed = true;
					while (blocks.Count > 0)
					{
						blocks.Dequeue().buffer.Release();
					}

					if (waitHandle != null)
						waitHandle.Dispose();
				}
			}
		}
	}
}
