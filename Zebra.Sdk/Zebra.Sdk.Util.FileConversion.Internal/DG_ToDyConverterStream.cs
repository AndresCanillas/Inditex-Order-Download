using System;
using System.IO;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class DG_ToDyConverterStream : MemoryStream
	{
		private Stream sourceStream;

		private string fakeDyHeader = string.Empty;

		private int readCounter;

		public DG_ToDyConverterStream(Stream sourceStream)
		{
			Stream stream = sourceStream;
			if (stream == null)
			{
				throw new IOException("input stream is null");
			}
			this.sourceStream = stream;
			string[] strArrays = new string[] { "", "", "" };
			int num = 0;
			int num1 = 0;
			while (num1 != 3 && num != -1)
			{
				num = sourceStream.ReadByte();
				if (num == 44 || num == ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR)
				{
					num1++;
				}
				else
				{
					ref string strPointers = ref strArrays[num1];
					char chr = (char)num;
					strPointers = string.Concat(strPointers, chr.ToString());
				}
			}
			if (num1 != 3)
			{
				throw new IOException("Invalid ~DG Header");
			}
			strArrays[0] = strArrays[0].Replace(string.Concat(ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX, "DG"), "~DY");
			strArrays[0] = strArrays[0].Replace("~DG", "~DY").Trim();
			this.fakeDyHeader = string.Concat(new string[] { strArrays[0], ",A,G,", strArrays[1], ",", strArrays[2], "," });
			this.fakeDyHeader = ZPLUtilities.ReplaceAllWithInternalCharacters(this.fakeDyHeader);
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
			if (this.readCounter >= this.fakeDyHeader.Length)
			{
				return this.sourceStream.ReadByte();
			}
			string str = this.fakeDyHeader;
			int num = this.readCounter;
			this.readCounter = num + 1;
			return str[num];
		}
	}
}