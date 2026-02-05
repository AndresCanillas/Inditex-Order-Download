using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	//public class MachineLock : IDisposable
	//{
	//	private bool acquiredLock = false;
	//	private Mutex mutex;

	//	public MachineLock(string mutexName)
	//	{
	//		if (String.IsNullOrWhiteSpace(mutexName))
	//			throw new Exception("mutexName argument cannot be null or empty");

	//		bool createdNew;
	//		MutexAccessRule everyoneRule = new MutexAccessRule(
	//			new SecurityIdentifier(WellKnownSidType.WorldSid, null),
	//			MutexRights.FullControl, AccessControlType.Allow);
	//		MutexSecurity security = new MutexSecurity();
	//		security.AddAccessRule(everyoneRule);
	//		mutex = new Mutex(false, "Global\\MachineLock_" + mutexName, out createdNew, security);
	//		try
	//		{
	//			if (mutex.WaitOne(10000, false))
	//			{
	//				acquiredLock = true;
	//			}
	//			else
	//			{
	//				acquiredLock = false;
	//				throw new Exception("Cannot acquire exclusive access to the resource.");
	//			}
	//		}
	//		catch (AbandonedMutexException)
	//		{
	//			// Mutex was abandoned by another process, but we have it now... Ok to continue.
	//			acquiredLock = true;
	//		}
	//	}

	//	public void Dispose()
	//	{
	//		if (acquiredLock)
	//			mutex.ReleaseMutex();
	//		mutex.Dispose();
	//	}
	//}


	public class SharedLock : IDisposable
	{
		class LockInfo
		{
			public int Count;
			public string LockName;
			public AutoResetEvent waitHandle;

			public LockInfo(string lockname)
			{
				LockName = lockname;
				waitHandle = new AutoResetEvent(true);
			}
		}

		private static object syncObj = new object();
		private static Dictionary<string, LockInfo> locks = new Dictionary<string, LockInfo>();
	
		public static SharedLock Acquire(string lockName)
		{
			var lockInfo = AcquireLockInfo(lockName);
			return new SharedLock(lockInfo);
		}

		public static SharedLock Acquire(string lockName, TimeSpan timeout)
		{
			SharedLock slock;
			var lockInfo = AcquireLockInfo(lockName);
			try
			{
				slock = new SharedLock(lockInfo, timeout);
				return slock;
			}
			catch
			{
				ReleaseLockInfo(lockInfo);
				throw;
			}
		}

		public static bool TryAcquire(string lockName, TimeSpan timeout, out SharedLock slock)
		{
			var lockInfo = AcquireLockInfo(lockName);
			slock = new SharedLock(lockInfo, timeout, false);
			if (slock.Acquired)
			{
				return true;
			}
			else
			{
				ReleaseLockInfo(lockInfo);
				return false;
			}
		}

		private static LockInfo AcquireLockInfo(string lockName)
		{
			LockInfo lck;
			lock (syncObj)
			{
				if (!locks.TryGetValue(lockName, out lck))
				{
					lck = new LockInfo(lockName);
					locks.Add(lockName, lck);
				}
				lck.Count++;
			}
			return lck;
		}

		private static void ReleaseLockInfo(LockInfo lck)
		{
			lock (syncObj)
			{
				lck.Count--;
				if (lck.Count == 0)
					locks.Remove(lck.LockName);
			}
		}


		private LockInfo lockInfo;
		private bool acquired;

		public bool Acquired { get => acquired; }

		private SharedLock(LockInfo lockInfo)
		{
			this.lockInfo = lockInfo;
			if (!lockInfo.waitHandle.WaitOne(50000))
			{
				acquired = false;
				throw new Exception("Exclusive lock could not be acquired.");
				//Dispose();
			}
			else acquired = true;
		}

		private SharedLock(LockInfo lockInfo, TimeSpan timeout, bool throwIfNotAcquired = true)
		{
			this.lockInfo = lockInfo;
			if (!lockInfo.waitHandle.WaitOne(timeout))
			{
				acquired = false;
				if (throwIfNotAcquired)
					throw new Exception("Exclusive lock could not be acquired.");
			}
			else acquired = true;
		}

		public void Dispose()
		{
			if (acquired)
				lockInfo.waitHandle.Set();
			ReleaseLockInfo(lockInfo);
		}
	}


	public static class WaitHandleExtensions
	{
		public static async Task<bool> WaitOneAsync(this WaitHandle handle, int millisecondsTimeout, CancellationToken cancellationToken)
		{
			RegisteredWaitHandle registeredHandle = null;
			CancellationTokenRegistration tokenRegistration = default(CancellationTokenRegistration);
			try
			{
				var tcs = new TaskCompletionSource<bool>();
				registeredHandle = ThreadPool.RegisterWaitForSingleObject(
					handle,
					(state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut),
					tcs,
					millisecondsTimeout,
					true);
				tokenRegistration = cancellationToken.Register(
					state => ((TaskCompletionSource<bool>)state).TrySetCanceled(),
					tcs);
				return await tcs.Task;
			}
			finally
			{
				if (registeredHandle != null)
					registeredHandle.Unregister(null);
				tokenRegistration.Dispose();
			}
		}

		public static Task<bool> WaitOneAsync(this WaitHandle handle, TimeSpan timeout, CancellationToken cancellationToken)
		{
			return handle.WaitOneAsync((int)timeout.TotalMilliseconds, cancellationToken);
		}

		public static Task<bool> WaitOneAsync(this WaitHandle handle, CancellationToken cancellationToken)
		{
			return handle.WaitOneAsync(Timeout.Infinite, cancellationToken);
		}
	}
}
