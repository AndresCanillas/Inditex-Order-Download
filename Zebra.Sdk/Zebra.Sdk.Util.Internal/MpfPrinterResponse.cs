using Newtonsoft.Json;
using System;

namespace Zebra.Sdk.Util.Internal
{
	[JsonObject]
	internal class MpfPrinterResponse
	{
		private string filename;

		private long size;

		private long crc32;

		public long Crc32
		{
			get
			{
				return this.crc32;
			}
			set
			{
				this.crc32 = value;
			}
		}

		public string Filename
		{
			get
			{
				return this.filename;
			}
			set
			{
				this.filename = value;
			}
		}

		public long Size
		{
			get
			{
				return this.size;
			}
			set
			{
				this.size = value;
			}
		}

		public MpfPrinterResponse()
		{
		}
	}
}