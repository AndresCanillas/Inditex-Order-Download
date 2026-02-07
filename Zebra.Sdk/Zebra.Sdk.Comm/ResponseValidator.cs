using System;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       An interface defining a method to validate whether a response from the printer is complete.
	///       </summary>
	public interface ResponseValidator
	{
		/// <summary>
		///       Provide a method to determine whether a response from the printer is a complete response.
		///       </summary>
		/// <param name="input">string to be validated</param>
		/// <returns>true if the string is a complete response</returns>
		bool IsResponseComplete(byte[] input);
	}
}