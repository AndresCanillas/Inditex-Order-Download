using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public enum RQLifetime
	{
		Roundtrip,	// RequestInfo object will live until a message comes back from the remote endpoint (or the connection is closed). This is used exclusively for method calls.
		Oneway		// RequestInfo object will live only until the output message is sent to the remote end point, this is used for every other message that is not a method invocation.
	}

	/// <summary>
	/// Describes an ongoing request sent to the other end point of a connection
	/// </summary>
	public class RequestInfo : IDisposable
	{
		public static TimeSpan MAX_REQUEST_TIMEOUT = TimeSpan.FromMinutes(5);

		private static int currentid;
		private static int NextID()
		{
			return Interlocked.Increment(ref currentid);
		}

		private object syncObj = new object();

		private bool disposed;
		private ManualResetEvent waitHandle;
		private bool sending;

		public IMsgSession session;
		public int msgid;
		public RQLifetime lifetime;
		public ProtocolBuffer output;
		public ProtocolBuffer input;
		public Exception error;


		public RequestInfo() { }


		public RequestInfo(IMsgSession session)
		{
			this.session = session;
			msgid = NextID();
			lifetime = RQLifetime.Oneway;
			output = new ProtocolBuffer(session.Scope);
		}


		public RequestInfo(IMsgSession session, bool asyncCall)
		{
			this.session = session;
			msgid = NextID();
			lifetime = RQLifetime.Roundtrip;
			output = new ProtocolBuffer(session.Scope);

			if (asyncCall)
				output.StartMessage(MsgOpcode.InvokeAsync, msgid);
			else
				output.StartMessage(MsgOpcode.Invoke, msgid);

			waitHandle = new ManualResetEvent(false);
		}


		public RequestInfo(IMsgSession session, ProtocolBuffer message)
		{
			this.session = session;
			msgid = message.msgid;
			lifetime = RQLifetime.Oneway;
			input = message;
			output = new ProtocolBuffer(session.Scope);
			output.StartMessage(MsgOpcode.Response, msgid);
		}


		public RequestInfo(IMsgSession session, EventData evt)
		{
			this.session = session;
			msgid = 0;
			lifetime = RQLifetime.Oneway;
			output = evt.buffer.MakeCopy();
		}


		public void SendRequest()
		{
			try
			{
				session.SendRequest(this);

				if (!waitHandle.WaitOne(MAX_REQUEST_TIMEOUT))
					throw new Exception("Timed out while waiting for the remote end point to respond.");
			}
			catch(Exception ex)
            {
				lock(syncObj)
					error = ex;
            }

			lock (syncObj)
			{
				if (error != null)
					throw error;
			}
		}


		public async Task SendVoidRequestAsync()
		{
			try
			{
				try
				{
					session.SendRequest(this);

					if (!await waitHandle.WaitOneAsync(MAX_REQUEST_TIMEOUT, CancellationToken.None))
						throw new Exception("Timed out while waiting for the remote end point to respond.");
				}
				catch (Exception ex)
				{
					lock(syncObj)
						error = ex;
				}

				lock (syncObj)
				{
					if (error != null)
						throw error;
				}
			}
			finally
			{
				Dispose();
			}
		}



		public async Task<T> SendTypedRequestAsync<T>()
		{
			try
			{
				try
				{
					session.SendRequest(this);

					if (!await waitHandle.WaitOneAsync(MAX_REQUEST_TIMEOUT, CancellationToken.None))
						throw new Exception("Timed out while waiting for the remote end point to respond.");
				}
				catch (Exception ex)
				{
					lock(syncObj)
						error = ex;
				}

				lock (syncObj)
				{
					if (error != null)
						throw error;

					var result = input.GetNext<T>();
					return result;
				}
			}
			finally
			{
				Dispose();
			}
		}


		public void SetResponse(ProtocolBuffer response)
		{
			lock (syncObj)
			{
				if (!disposed)
				{
					input = response;
					waitHandle.Set();
				}
				else response.Release();
			}
		}


		public void SetError(Exception ex)
        {
			lock (syncObj)
			{
				if (!disposed)
				{
					error = ex;
					waitHandle.Set();
				}
			}
		}


		public void Lock()
        {
			lock (syncObj)
				sending = true;
        }

		public void Unlock()
		{
			lock (syncObj)
			{
				sending = false;
				if (disposed)
					Dispose();
			}
		}

		public void Dispose()
		{
			lock (syncObj)
			{
				if (sending)
				{
					disposed = true;
					return;
				}

				disposed = true;

				if (waitHandle != null)
					waitHandle.Dispose();

				if (output != null)
					output.Release();

				if (input != null)
					input.Release();

				output = null;
				input = null;
				waitHandle = null;
				error = null;
			}
		}
	}
}
