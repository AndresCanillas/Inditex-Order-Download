using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Service.Contracts
{
	public interface IUserMemoryMethod
	{
		bool WriteUserMemory { get; }
		bool IsCompatible(ITagEncoding encoding);
		string GetContent(ITagEncoding encoding, JObject data);
	}
}
