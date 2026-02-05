using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.PrintCentral
{
	public interface ISerialNumberClient
	{
		string Url { get; set; }
		string Token { get; set; }
		bool Authenticated { get; }
		void Login(string loginUrl, string userName, string password);
		Task LoginAsync(string loginUrl, string userName, string password);
		long GetSerials(GetSerialsRQ request);
	}

	public class GetSerialsRQ
	{
		public SerialSequenceInfo Sequence;
		public int Count;
	}

	public class SerialNumberClient : BaseServiceClient, ISerialNumberClient
	{
		public long GetSerials(GetSerialsRQ request)
		{
			return Invoke<GetSerialsRQ, long>("serialnumber/getserials", request);
		}
	}
}
