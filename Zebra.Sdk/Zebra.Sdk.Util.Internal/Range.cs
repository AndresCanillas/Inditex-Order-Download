using System;

namespace Zebra.Sdk.Util.Internal
{
	internal class Range
	{
		private int begin;

		private int end;

		public Range(int begin, int end)
		{
			this.begin = begin;
			this.end = end;
		}

		public bool ContainsInt(int x)
		{
			if (x < this.begin)
			{
				return false;
			}
			return x <= this.end;
		}
	}
}