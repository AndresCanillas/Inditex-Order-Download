using System;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Settings.Internal
{
	internal class JsonValidator : ResponseValidator
	{
		public JsonValidator()
		{
		}

		public bool IsResponseComplete(byte[] input)
		{
			return JsonHelper.IsValidJson(input);
		}
	}
}