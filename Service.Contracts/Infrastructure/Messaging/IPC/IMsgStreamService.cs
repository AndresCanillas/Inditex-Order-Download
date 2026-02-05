using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	// This service stores references to streams that are available for processing within a given session.
	// It must be registered as a scoped service within the scope associated to the messaging session.
	//
	// When the session ends, the scope must be disposed to ensure all scoped services are disposed along
	// with the scope.

	public interface IMsgStreamService : IDisposable
	{
		void RegisterStream(Guid guid, Stream stream);
		Guid RegisterStream(Stream stream);
		Stream GetStream(Guid guid);
		void UnregisterStream(Guid guid);
	}


	public class MsgStreamService : IMsgStreamService
	{
		private ConcurrentDictionary<Guid, StreamInfo> streams;
		private Timer purgeTimer;
		private ILogService log;

		public MsgStreamService(ILogService log)
		{
			this.log = log;
			streams = new ConcurrentDictionary<Guid, StreamInfo>();
			purgeTimer = new Timer(DisposeAbandonedStreams, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
		}


		public void Dispose()
		{
			foreach(var sinfo in streams.Values)
				sinfo.Stream.Dispose();
			streams.Clear();
		}


		public void RegisterStream(Guid id, Stream stream)
		{
			if (!streams.TryAdd(id, new StreamInfo(id, stream)))
				throw new Exception("Received a duplicate Stream Guid");
		}


		public Guid RegisterStream(Stream stream)
		{
			Guid id;
			do
			{
				id = Guid.NewGuid();
			} while (!streams.TryAdd(id, new StreamInfo(id, stream)));
			return id;
		}


		public Stream GetStream(Guid guid)
		{
			StreamInfo s;
			if (streams.TryGetValue(guid, out s))
			{
				s.TTL += 10;
				return s.Stream;
			}
			else
				return null;
		}


		public void UnregisterStream(Guid guid)
		{
			if (streams.TryRemove(guid, out var sinfo))
			{
				try
				{
					if (sinfo != null)
					{
						if (sinfo.Stream != null)
							sinfo.Stream.Dispose();
						sinfo.Stream = null;
					}
				}
				catch { }
			}
		}

		private void DisposeAbandonedStreams(object state)
		{
			foreach(var sinfo in streams.Values)
			{
				sinfo.TTL--;
				if(sinfo.TTL <= 0)
				{
					UnregisterStream(sinfo.ID);
					log.LogMessage("Disposed abandoned stream");
				}
			}
		}
	}


	public class StreamInfo
	{
		public Guid ID;
		public Stream Stream;
		public volatile int TTL;

		public StreamInfo(Guid id, Stream stream)
		{
			ID = id;
			Stream = stream;
			TTL = 10;
		}
	}
}
