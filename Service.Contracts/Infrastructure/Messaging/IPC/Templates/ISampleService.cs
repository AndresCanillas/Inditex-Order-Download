using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	enum StatusEnum
	{
		AllGood,
		Errr,
		WhoKnows
	}

	class StatusData
	{
		public int ID;
		public string Name { get; set; }
		public StatusEnum Status;
	}

	interface ISampleService
	{
		event EventHandler<int> TheEvent;
		event EventHandler<StatusData> AnotherEvent;
		int DoSomething(string data);
		void SetStatus(StatusData status);
		StatusData GetStatus();
		Task AddUserAsync(string username, string password);
		Task<string> DoSomethingAsync(string data);
	}

	class ServiceImpl : ISampleService, IDisposable
	{
		public event EventHandler<int> TheEvent { add { } remove { } }

		public event EventHandler<StatusData> AnotherEvent { add { } remove { } }


		public void Dispose()
		{
			// dispose the object... in this example we dont have anything to do...
		}

		public int DoSomething(string data)
		{
			// Execute something...

			// Then return an int
			return 10;
		}

		public void SetStatus(StatusData status)
		{
			
		}

		public StatusData GetStatus()
		{
			return new StatusData();
		}


		public async Task AddUserAsync(string username, string password)
		{
			// simulate some work
			await Task.Delay(10);
		}

		public async Task<string> DoSomethingAsync(string data)
		{
			// Code of the async method...
			// ...
			await Task.Delay(14);  // simulate some work

			return "Task Completed OK";
		}
	}
}
