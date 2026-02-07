using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
	public class SerialNumberRepository : ISerialRepository
	{
		private ILogService log;
		private IDBConnectionManager connManager;
		private ConcurrentDictionary<string, SerialCache> store;        // In memory storage spanning all existing serial sequences in the system. These are loaded and initialized on demand. 
																		// The key for this dictionary is composed of a combination of the sequence ID and filter fields.

		public SerialNumberRepository(
			ILogService log,
			IDBConnectionManager connManager)
		{
			this.log = log;
			this.connManager = connManager;
			store = new ConcurrentDictionary<string, SerialCache>();
		}


		// Acquires one serial number, this is ideal for printing processes that do not require to pre-allocate all the
		// serial numbers in advance, like when printing from the web.
		//
		// This method also reuses returned serials (serials are returned to the in memory cache by calling ReuseSerial).
		// Two things worth noticing with this method are:
		//		- Serials are allocated from the sequence (DB table SerialSequences) and stored in the in memory cache.
		//		  This is done to increase performance, as otherwise, we would have to hit the DB for each serial allocated.
		//		- Serials are pre-loaded in background once the cache has less than 25 serials left. Again this aims at 
		//		  eliminating wait times while requesting serials using this method.
		public long AcquireSerial(SerialSequenceInfo sq)
		{
			long result;
			SerialCache cache = GetCache(sq);
			while (!cache.Serials.TryDequeue(out result))
			{
				if (!PopulateSerialCache(sq, 50, cache))
					Thread.Sleep(50);
			}
			if (cache.Serials.Count < 25)
				Task.Run(() => PopulateSerialCache(sq, 25, cache));
			return result;
		}


		// This method acquires many (count) serials from directly from the database (table SerialSequences). This is the
		// best method to use when we will be printing offline and need to allocate all the serials ahead of time.
		// Because all serials allocated need to be consecutive, this method cannot reuse returned serials.
		public List<long> AcquireMultiple(SerialSequenceInfo sq, int count)
		{
			var serial = AllocateSerials(sq, count);
			var serials = new List<long>(count);
			for (var i = 0; i < count; i++)
				serials.Add(serial + i);
			return serials;
		}


		// Acquires multiple serials as a sequential block starting at the returned "Serial" up to "Serial" + Count - 1.
		public long AcquireSequential(SerialSequenceInfo sq, int count)
		{
			return AllocateSerials(sq, count);
		}


		// Returns the serial number back to the in-memory cache where it can be reused later. Care must be taken not
		// to return serials unless we are certain that the serial in question was not encoded in the intended tag.
		// This can happen for instance, if the printer reported an error while trying to print the label, or if
		// a validation process (involving an RFID reader) confirms that a tag was not properly encoded. Also it is
		// important to only use this on serials allocated using AcquireSerial, serials allocated with AcquireMultiple
		// should never be returned.
		public void ReuseSerial(SerialSequenceInfo sq, long serial)
		{
			SerialCache cache = GetCache(sq);
			cache.Serials.Enqueue(serial);
		}


		// Gets a sample serial from the specified sequence. The sequence is not updated in the process.
		public long GetSample(SerialSequenceInfo sq)
		{
			var ssq = ReadSequenceValue(sq);
			return ssq.NextValue;
		}


		// Updates the current value of the sequence. The supplied value will be the next serial allocated from the sequence.
		// There are several concurrency checks in place here:
		//		- When we query the sequence, there is a chance that the sequence has not been setup yet in the DB, 
		//		  this is taken care of in ReadSequenceValue.
		//		- Another possible race is when we update the sequence value.
		//
		// In all cases we check for concurrent access by including the NextValue in the where clause, and checking to see how many rows were updated.
		// If zero rows were affected by the update, then that means there was a concurrent access and the entire process should be retried once more.
		// In case of collision, the thread will wait a small ammount of time before retrying. A maximum of 10 consecutive attempts will be made, if after
		// 10 attempts we are unable to properly update the sequence, then an error is thrown.
		public void SetCurrent(SerialSequenceInfo sq, long value)
		{
			int rows, retryCount = 0, waitTime = 20;
			long oldValue;
			using (IDBX conn = connManager.OpenWebLinkDB())
			{
				do
				{
					var current = ReadSequenceValue(sq);
					oldValue = current.NextValue;
					var filter = sq.GetNormalizedFilter();
					rows = conn.ExecuteNonQuery("update SerialSequences set NextValue = @val where ID = @id and Filter = @filter and NextValue = @oldVal", value, sq.ID, filter, oldValue);
					if (rows == 0)
					{
						Thread.Sleep(waitTime);
						waitTime += 10;
					}
				} while (rows == 0 && ++retryCount < 10);
				if (rows == 0)
					throw new Exception("Could not set sequence value due to concurrency.");
			}
		}


		private SerialCache GetCache(SerialSequenceInfo sq)
		{
			SerialCache cache;
			string cacheKey = sq.ID + "/" + sq.GetNormalizedFilter();
			if (!store.TryGetValue(cacheKey, out cache))
			{
				cache = new SerialCache();
				if (!store.TryAdd(cacheKey, cache))
					cache = store[cacheKey];
			}
			return cache;
		}


		private bool PopulateSerialCache(SerialSequenceInfo sq, int count, SerialCache cache)
		{
			bool invokeAllocate = false;
			lock (cache.syncObj)
			{
				if (!cache.Allocating)
				{
					cache.Allocating = true;
					invokeAllocate = true;
				}
			}
			if (invokeAllocate)
			{
				var serial = AllocateSerials(sq, count);
				for (int i = 0; i < count; i++)
					cache.Serials.Enqueue(serial + i);
				cache.Allocating = false;
				return true;
			}
			return false;
		}


		private long AllocateSerials(SerialSequenceInfo sq, int count)
		{
			int rows, retryCount = 0, waitTime = 20;
			long serial = -1;
			using(IDBX conn = connManager.OpenWebLinkDB())
			{
				do
				{
					var val = ReadSequenceValue(sq);
					serial = val.NextValue;
					var filter = sq.GetNormalizedFilter();
                    if ((serial + count) > sq.MaxValue)
                        throw new Exception($"Could not get serial from server, quantity serial requested '{count}' is not available");                        
					rows = conn.ExecuteNonQuery("update SerialSequences set NextValue = @val where ID = @id and Filter = @filter and NextValue = @oldVal", serial + count, sq.ID, filter, serial);
					if (rows == 0)
					{
						serial = -1;
						Thread.Sleep(waitTime);
						waitTime += 10;
					}
				} while (rows == 0 && ++retryCount < 10);
                if (rows == 0 || serial == -1)
                    throw new Exception("Could not get serial from sequence due to concurrency.");
			}
			return serial;
		}


		// Reads the current value of a given sequence. This method handles concurrency in the case the sequence in question is not
		// initialized yet and needs to be inserted. If multiple threads are competing for this case, then only one thread is guaranted
		// to succed at inserting, the others will get a duplicate key error instead.
		private SerialSQ ReadSequenceValue(SerialSequenceInfo sq)
		{
			SerialSQ value;
			using (IDBX conn = connManager.OpenWebLinkDB())
			{
				var filter = sq.GetNormalizedFilter();
				value = conn.SelectOne<SerialSQ>("select * from SerialSequences where ID = @id and Filter = @filter", sq.ID, filter);
				if (value == null)
				{
					try
					{
						value = new SerialSQ() { ID = sq.ID, Filter = filter, NextValue = sq.SequenceStart };
						conn.Insert(value);
						return value;
					}
					catch (Exception ex) when (ex.Message.ToLowerInvariant().Contains("duplicate key"))
					{
						// Ignore duplicate key error, just means someone else initialized this sequence concurrently.
						log.LogException(ex);
						value = conn.SelectOne<SerialSQ>("select * from SerialSequences where ID = @id and Filter = @filter", sq.ID, filter);
					}
				}
			}
			return value;
		}

		public void Preallocate(SerialSequenceInfo sq, int count)
		{
			// NOTE: Intentionally left empty. In PrintCentral we dont have to preallocate serials
		}
	}

	[TargetTable("SerialSequences")]
	class SerialSQ
	{
		public string ID;
		public string Filter;
		public long NextValue;
	}


	class SerialCache
	{
		public object syncObj = new object();
		public ConcurrentQueue<long> Serials = new ConcurrentQueue<long>();
		public volatile bool Allocating = false;
	}
}
