using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	[Serializable]
	public class BaseServiceResponse
	{
		public bool Success;
		public string ErrorMessage;
	}
}
