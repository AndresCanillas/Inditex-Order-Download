using System;

namespace Services.Core
{
	[Serializable]
	public class AuthorizationException : Exception
	{
		public AuthorizationException(string message) : base(message)
		{
		}
	}
}
