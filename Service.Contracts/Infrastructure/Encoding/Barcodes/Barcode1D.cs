using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	public interface IBarcode1D
	{
		string Code { get; set; }
		char CheckDigit { get; set; }
		void Encode(ITagEncoding tag, string code);
	}

	public abstract class Barcode1D
	{
		private string code;

		public virtual string Code
		{
			get { return code; }
			set
			{
				CodeUpdated(value);
				code = value;
			}
		}

		public char CheckDigit { get; set; }

		public abstract void Encode(ITagEncoding tag, string code);

		public virtual void CodeUpdated(string code) { }

		public virtual char GetCheckDigit(string code)
		{
			return GS1Mod(code);
		}

		public static char GS1Mod(string code)
		{
			var charTable = "01234567890";
			if (code.Length != 7 && code.Length != 11 && code.Length != 12 && code.Length != 13 && code.Length != 16 && code.Length != 17)
				throw new InvalidOperationException("Invalid code, expected at least 7 digits (GTIN-8 code) or up to 17 digits (SSCC code). NOTE: Check digit MUST NOT be included for this calculation.");
			int[] paddedCode = new int[18];
			for (int i = 17 - code.Length, c = 0; c < code.Length; i++, c++)
				paddedCode[i] = charTable.IndexOf(code[c]);
			for (int i = 0; i < 17; i += 2)
				paddedCode[i] *= 3;
			var sum = 0;
			for (int i = 0; i < 17; i++)
				sum += paddedCode[i];
			return charTable[10 - sum % 10];
		}
	}
}
