using System;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class EnumAttributes
	{
		private int segment;

		private int @value;

		private string description;

		public int Segment
		{
			get
			{
				return this.segment;
			}
		}

		public int Value
		{
			get
			{
				return this.@value;
			}
		}

		protected EnumAttributes()
		{
		}

		public EnumAttributes(string description) : this(0, 0, description)
		{
		}

		public EnumAttributes(int value, string description) : this(0, value, description)
		{
		}

		public EnumAttributes(int segment, int value, string description)
		{
			this.segment = segment;
			this.@value = value;
			this.description = description;
		}

		public override string ToString()
		{
			return this.description;
		}
	}
}