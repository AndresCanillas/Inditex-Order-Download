using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class DYDataProviderStream : MemoryStream
	{
		private Stream sourceStream;

		private string[] headerParts = new string[] { "", "", "", "", "" };

		private byte[] preReaderDataHeader = new byte[5];

		private int preReaderDataHeaderIndex;

		private Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier dataFormatSpecifier = Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier.OTHER;

		public Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier DataFormatSpecifier
		{
			get
			{
				return this.dataFormatSpecifier;
			}
		}

		public string FileExtensionCode
		{
			get
			{
				return this.headerParts[2];
			}
		}

		public string FilenameOnPrinter
		{
			get
			{
				return this.headerParts[0];
			}
		}

		public string FormatDownloadedInDataField
		{
			get
			{
				return this.headerParts[1];
			}
		}

		public Stream SourceStream
		{
			get
			{
				return this.sourceStream;
			}
		}

		public DYDataProviderStream(Stream sourceStream)
		{
			this.sourceStream = sourceStream;
			int num = 0;
			int num1 = 0;
			while (num != 5 && num1 != -1)
			{
				num1 = this.sourceStream.ReadByte();
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
			if (num != 5)
			{
				throw new IOException("Invalid ~DY Header");
			}
			this.headerParts[0] = this.headerParts[0].Replace(string.Concat(ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX, "DY"), "");
			this.headerParts[0] = this.headerParts[0].Replace("~DY", "").Trim();
			this.CheckAndAdjustFileNameAndExtensionCode();
			this.AdjustDataFormatSpecifier();
			for (int i = 0; i < (int)this.preReaderDataHeader.Length; i++)
			{
				this.preReaderDataHeader[i] = (byte)sourceStream.ReadByte();
			}
			this.SetDataFormatSpecifier();
			if (this.IsDataMimed())
			{
				this.IgnorePrereadData();
			}
		}

		private void AdjustDataFormatSpecifier()
		{
			if ((new List<string>(new string[] { "B", "E", "T", "NRD", "PAC" })).Contains(this.headerParts[2].ToUpper()))
			{
				this.headerParts[1] = "B";
			}
		}

		private void CheckAndAdjustFileNameAndExtensionCode()
		{
			string upper = "G";
			Dictionary<string, string> strs = new Dictionary<string, string>()
			{
				{ "BMP", "B" },
				{ "TTE", "E" },
				{ "GRF", "G" },
				{ "PNG", "P" },
				{ "TTF", "T" },
				{ "PCX", "X" },
				{ "NRD", "NRD" },
				{ "PAC", "PAC" }
			};
			List<string> strs1 = new List<string>(new string[] { "B", "E", "G", "P", "T", "X", "NRD", "PAC" });
			string[] strArrays = Regex.Split(this.headerParts[0], "\\.");
			if (strs1.Contains(this.headerParts[2].ToUpper()))
			{
				upper = this.headerParts[2].ToUpper();
			}
			else if (2 == (int)strArrays.Length && 0 < strArrays[1].Length && strs.ContainsKey(strArrays[1].ToUpper()))
			{
				upper = strs[strArrays[1].ToUpper()];
			}
			this.headerParts[2] = upper;
			foreach (KeyValuePair<string, string> str in strs)
			{
				if (!str.Value.Equals(upper))
				{
					continue;
				}
				this.headerParts[0] = string.Concat(strArrays[0], ".", str.Key);
			}
		}

		public int GetBytesPerRow()
		{
			if (string.IsNullOrEmpty(this.headerParts[4]))
			{
				return -1;
			}
			int num = -1;
			try
			{
				num = int.Parse(this.headerParts[4]);
			}
			catch (FormatException)
			{
			}
			if (num < 0)
			{
				throw new IOException("Invalid ~DY Header");
			}
			return num;
		}

		public int GetTotalBytesInData()
		{
			int num = -1;
			try
			{
				num = (string.IsNullOrEmpty(this.headerParts[3]) ? -1 : int.Parse(this.headerParts[3]));
			}
			catch (FormatException)
			{
			}
			if (num < 0)
			{
				throw new IOException("Invalid ~DY Header");
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
			return Regex.IsMatch(Encoding.UTF8.GetString(this.preReaderDataHeader, 0, (int)this.preReaderDataHeader.Length), ":Z64:", RegexOptions.IgnoreCase);
		}

		private bool IsMimeUncompressed()
		{
			return Regex.IsMatch(Encoding.UTF8.GetString(this.preReaderDataHeader, 0, (int)this.preReaderDataHeader.Length), ":B64:", RegexOptions.IgnoreCase);
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
				byte[] numArray = this.preReaderDataHeader;
				int num1 = this.preReaderDataHeaderIndex;
				this.preReaderDataHeaderIndex = num1 + 1;
				num = numArray[num1];
			}
			return num;
		}

		private void SetDataFormatSpecifier()
		{
			if (Regex.IsMatch(this.FormatDownloadedInDataField, "B", RegexOptions.IgnoreCase))
			{
				this.dataFormatSpecifier = Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier.RAW_BINARY;
				return;
			}
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
			this.dataFormatSpecifier = Zebra.Sdk.Util.FileConversion.Internal.DataFormatSpecifier.ASCII_HEX;
		}
	}
}