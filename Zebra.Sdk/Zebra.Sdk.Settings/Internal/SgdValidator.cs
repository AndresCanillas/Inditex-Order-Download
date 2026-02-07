using System;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Settings.Internal
{
	internal class SgdValidator : ResponseValidator
	{
		public SgdValidator()
		{
		}

		public bool IsResponseComplete(byte[] input)
		{
			if (input[0] != 34)
			{
				return false;
			}
			return input[(int)input.Length - 1] == 34;
		}
	}
}