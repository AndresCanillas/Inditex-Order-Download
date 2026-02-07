using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class DZ_DataProviderStream : MemoryStream
	{
		private Stream sourceStream;

		private string[] headerParts = new string[] { "", "" };

		private int[] preReaderDataHeader = new int[5];

		private int preReaderDataHeaderIndex;

		private Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier dataFormatSpecifier = Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier.OTHER;

		public Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier DataFormatSpecifier
		{
			get
			{
				return this.dataFormatSpecifier;
			}
		}

		public string FilenameOnPrinter
		{
			get
			{
				return this.headerParts[0];
			}
		}

		public Stream SourceStream
		{
			get
			{
				return this.sourceStream;
			}
		}

		public DZ_DataProviderStream(Stream sourceStream)
		{
			this.sourceStream = sourceStream;
			int num = 0;
			int num1 = 0;
			while (num != 2 && num1 != -1)
			{
				num1 = sourceStream.ReadByte();
				if (num1 == 44 || num1 == ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR)
				{
					num++;
				}
				else
				{
					ref string strPointers = ref this.headerParts[num];
					char chr = (char)num1;
					strPointers = string.Concat(strPointers, chr.ToString());
				}
			}
			if (num != 2)
			{
				throw new IOException("Invalid ~DZ Header");
			}
			this.headerParts[0] = this.headerParts[0].Replace(ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX, "~").Replace("~DZ", "").Trim();
			for (int i = 0; i < (int)this.preReaderDataHeader.Length; i++)
			{
				this.preReaderDataHeader[i] = sourceStream.ReadByte();
			}
			this.SetDataFormatSpecifier();
			if (this.IsDataMimed())
			{
				this.IgnorePrereadData();
			}
		}

		public int GetTotalBytesInData()
		{
			int num = -1;
			try
			{
				num = (string.IsNullOrEmpty(this.headerParts[1]) ? -1 : int.Parse(this.headerParts[1]));
			}
			catch (Exception)
			{
			}
			if (num < 0)
			{
				throw new IOException("Invalid ~DZ Header");
			}
			return num;
		}

		private void IgnorePrereadData()
		{
			this.preReaderDataHeaderIndex = (int)this.preReaderDataHeader.Length;
		}

		private bool IsDataMimed()
		{
			if (this.dataFormatSpecifier == Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier.MIME_COMPRESSED)
			{
				return true;
			}
			return this.dataFormatSpecifier == Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier.MIME_UNCOMPRESSED;
		}

		private bool IsMimeCompressed()
		{
			byte[] array = (
				from i in (IEnumerable<int>)this.preReaderDataHeader
				select (byte)i).ToArray<byte>();
			return Regex.IsMatch(Encoding.UTF8.GetString(array), ":Z64:", RegexOptions.IgnoreCase);
		}

		private bool IsMimeUncompressed()
		{
			byte[] array = (
				from i in (IEnumerable<int>)this.preReaderDataHeader
				select (byte)i).ToArray<byte>();
			return Regex.IsMatch(Encoding.UTF8.GetString(array), ":B64:", RegexOptions.IgnoreCase);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int num = this.ReadByte();
			if (num == -1)
			{
				return -1;
			}
			buffer[offset] = (byte)num;
			int num1 = 1;
			try
			{
				while (num1 < count)
				{
					num = this.ReadByte();
					if (num == -1)
					{
						break;
					}
					buffer[offset + num1] = (byte)num;
					num1++;
				}
			}
			catch (Exception)
			{
			}
			return num1;
		}

		public override int ReadByte()
		{
			int num = -1;
			if (this.preReaderDataHeaderIndex >= (int)this.preReaderDataHeader.Length)
			{
				num = this.sourceStream.ReadByte();
			}
			else
			{
				int[] numArray = this.preReaderDataHeader;
				int num1 = this.preReaderDataHeaderIndex;
				this.preReaderDataHeaderIndex = num1 + 1;
				num = numArray[num1];
			}
			return num;
		}

		private void SetDataFormatSpecifier()
		{
			if (this.IsMimeUncompressed())
			{
				this.dataFormatSpecifier = Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier.MIME_UNCOMPRESSED;
				return;
			}
			if (this.IsMimeCompressed())
			{
				this.dataFormatSpecifier = Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier.MIME_COMPRESSED;
				return;
			}
			this.dataFormatSpecifier = Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier.RAW_BINARY;
		}
	}
}