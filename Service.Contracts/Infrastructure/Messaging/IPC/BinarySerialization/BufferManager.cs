using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public interface IBufferManager : IDisposable
	{
		byte[] AcquireSmallBuffer();
		byte[] AcquireBuffer(int requiredSize);
		void ReleaseBuffer(byte[] buffer);
		void GetCounters(out int small, out int medium, out int large, out int extra, out int other, out int lost);
	}

	public class BufferManager : IBufferManager
	{
		public const int SMALL_BUFFER_SIZE = 10000;
		public const int MEDIUM_BUFFER_SIZE = 20000;
		public const int LARGE_BUFFER_SIZE = 30000;
		public const int EXTRA_BUFFER_SIZE = 60000;

		private ConcurrentQueue<byte[]> smallBuffers;
		private ConcurrentQueue<byte[]> mediumBuffers;
		private ConcurrentQueue<byte[]> largeBuffers;
		private ConcurrentQueue<byte[]> extraBuffers;

		private object syncObj = new object();
		private bool disposed;
		private int smallcount = 10;
		private int mediumcount = 5;
		private int largecount = 1;
		private int extracount = 0;
		private int othercount = 0;
		private ILogService log;

		public BufferManager(ILogService log)
		{
			this.log = log;
			smallBuffers = new ConcurrentQueue<byte[]>();
			mediumBuffers = new ConcurrentQueue<byte[]>();
			largeBuffers = new ConcurrentQueue<byte[]>();
			extraBuffers = new ConcurrentQueue<byte[]>();

			for (int i = 0; i < smallcount; i++)
				smallBuffers.Enqueue(new byte[SMALL_BUFFER_SIZE]);

			for (int i = 0; i < mediumcount; i++)
				mediumBuffers.Enqueue(new byte[MEDIUM_BUFFER_SIZE]);

			for (int i = 0; i < largecount; i++)
				largeBuffers.Enqueue(new byte[LARGE_BUFFER_SIZE]);

			for (int i = 0; i < extracount; i++)
				extraBuffers.Enqueue(new byte[EXTRA_BUFFER_SIZE]);
		}


		public byte[] AcquireSmallBuffer()
		{
			byte[] result;

			if (!smallBuffers.TryDequeue(out result))
			{
				Interlocked.Increment(ref smallcount);
				return new byte[SMALL_BUFFER_SIZE];
			}
			else
				return result;
		}


		public byte[] AcquireBuffer(int requiredSize)
		{
			byte[] result;

			if (requiredSize <= SMALL_BUFFER_SIZE)
			{
				if (!smallBuffers.TryDequeue(out result))
				{
					Interlocked.Increment(ref smallcount);
					return new byte[SMALL_BUFFER_SIZE];
				}
				else
					return result;
			}

			if (requiredSize <= MEDIUM_BUFFER_SIZE)
			{
				if (!mediumBuffers.TryDequeue(out result))
				{
					Interlocked.Increment(ref mediumcount);
					return new byte[MEDIUM_BUFFER_SIZE];
				}
				else
					return result;
			}

			if (requiredSize <= LARGE_BUFFER_SIZE)
			{
				if (!largeBuffers.TryDequeue(out result))
				{
					Interlocked.Increment(ref largecount);
					return new byte[LARGE_BUFFER_SIZE];
				}
				else
					return result;
			}

			if (requiredSize <= EXTRA_BUFFER_SIZE)
			{
				if (!extraBuffers.TryDequeue(out result))
				{
					Interlocked.Increment(ref extracount);
					return new byte[EXTRA_BUFFER_SIZE];
				}
				else
					return result;
			}

			return new byte[(int)(requiredSize * 1.5)];
		}


		public void ReleaseBuffer(byte[] buffer)
		{
			if (buffer.Length > EXTRA_BUFFER_SIZE)
				return;  // We dont keep references to very large buffers, let the GC deal with that

			lock (syncObj)
			{
				if (disposed) return;

				if (buffer.Length == SMALL_BUFFER_SIZE)
					smallBuffers.Enqueue(buffer);

				if (buffer.Length == MEDIUM_BUFFER_SIZE)
					mediumBuffers.Enqueue(buffer);

				if (buffer.Length == LARGE_BUFFER_SIZE)
					largeBuffers.Enqueue(buffer);

				if (buffer.Length == EXTRA_BUFFER_SIZE)
					extraBuffers.Enqueue(buffer);
			}
		}


		public void GetCounters(out int small, out int medium, out int large, out int extra, out int other, out int lost)
		{
			small = smallcount;
			medium = mediumcount;
			large = largecount;
			extra = extracount;
			other = othercount;
			lost = smallcount - smallBuffers.Count + mediumcount - mediumBuffers.Count + largecount - largeBuffers.Count + extracount - extraBuffers.Count;
		}


		public void Dispose()
		{
			lock (syncObj)
			{
				if (disposed)
					return;

				disposed = true;

				GetCounters(out var c1, out var c2, out var c3, out var c4, out var c5, out var c6);
				if (c6 > 20)
					log.LogWarning("BufferManager detected some lost buffers, this issue might lead to more frequent Garbage Collections, which can impact performance");
			}

			while (smallBuffers.TryDequeue(out _)) ;
			while (mediumBuffers.TryDequeue(out _)) ;
			while (largeBuffers.TryDequeue(out _)) ;
			while (extraBuffers.TryDequeue(out _)) ;

			smallBuffers = null;
			mediumBuffers = null;
			largeBuffers = null;
			extraBuffers = null;
		}
	}
}
