using System;

namespace Services.Core
{
	[Serializable]
	public class MustChangePasswordException : Exception
	{
		public MustChangePasswordException(string message) : base(message)
		{
		}
	}
}
