using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Services.Zebra.Commands
{
	public class BaseCommand : IZCommand
	{
        private string message;
        private SemaphoreSlim responseSignal = new SemaphoreSlim(0, 1);
        private SemaphoreSlim transmissionSignal = new SemaphoreSlim(0, 1);
        private volatile string response;
        private volatile bool responseReady = false;
		private volatile Exception error;

		public BaseCommand() { }

		public void SetMessage(string message)
		{
			if (!message.EndsWith("\r\n"))
				message += "\r\n";
            this.message = message;
			error = null;
		}

		public string Message { get { return message; } }

		public virtual bool IsOneWay { get; set; } = true;

        public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

		public virtual void Reset()
		{
			error = null;
			responseReady = false;
			response = null;
		}

		public virtual byte[] ToByteArray()
		{
			Reset();
			return Encoding.UTF8.GetBytes(this.message);
		}

        public async Task WaitForTransmission()
        {
            await transmissionSignal.WaitAsync();
        }

        public async Task<string> WaitForResponse()
		{
            if (IsOneWay)
                throw new InvalidOperationException("This command is one way only, the server does not expect a response from the device.");
            if (responseReady)
				return GetResponse();
			bool signalSet = await responseSignal.WaitAsync(CommandTimeout);
            if (signalSet)
				return GetResponse();
			else
				throw new TimeoutException("Timed out while waiting for the device to respond.");
        }

		public async Task<string> WaitForResponse(TimeSpan timeout)
		{
			if (IsOneWay)
				throw new InvalidOperationException("This command is one way only, the server does not expect a response from the device.");
            if (responseReady)
				return GetResponse();
            bool signalSet = await responseSignal.WaitAsync(timeout);
            if (signalSet)
                return GetResponse();
            else
                throw new TimeoutException("Timed out while waiting for the device to respond.");
		}


		private string GetResponse()
		{
			var tmpException = error;
			error = null;
			if (tmpException != null)
			{
				throw tmpException;
			}
			else return response;
		}

		public virtual void SetResponse(string response)
		{
			this.response = response;
			error = null;
			responseReady = true;
			try
			{
				responseSignal.Release();
			}
			catch(SemaphoreFullException) { }
		}

		public virtual void SetError(Exception error)
		{
			this.response = null;
			this.error = error;
			responseReady = true;
			try
			{
				responseSignal.Release();
			}
			catch (SemaphoreFullException) { }
		}

		public void SetTransmission()
        {
            transmissionSignal.Release();
        }
    }
}
