using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	/// <summary>
	/// Allows to encode text using the EANUCC 128 alphabet
	/// </summary>
	public class EANUCC128Encoding
	{
		private const string ValidChars = "!\"%&'()*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
		private const string SymbolTable = "∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙∙!\"∙∙%&'()*+,-./0123456789:;<=>?∙ABCDEFGHIJKLMNOPQRSTUVWXYZ∙∙∙∙_∙abcdefghijklmnopqrstuvwxyz";

		/// <summary>
		/// Validates the given string to see if all its characters fall within the EANUCC128 alphabet.
		/// </summary>
		public static bool IsEANUCC128(string code)
		{
			if (code == null || code.Length == 0) return false;
			for (int i = 0; i < code.Length; i++)
			{
				if (ValidChars.IndexOf(code[i]) < 0)
					return false;
			}
			return true;
		}


		/// <summary>
		/// Returns the amount of bytes required to store the given code in EANUCC128 (each character requires 7 bits)
		/// </summary>
		public static int GetByteSize(string code)
		{
			return Convert.ToInt32(Math.Ceiling(code.Length * 0.875));
		}


		/// <summary>
		/// Encodes the given code using the EANUCC128 barcode symbology
		/// </summary>
		public static byte[] Encode(string code)
		{
			if(!IsEANUCC128(code))
				throw new ArgumentException("code has some invalid characters or is empty", "code");
			int size = GetByteSize(code);
			BitArray bits = new BitArray(size * 8, false);
			for (int i = 0; i < code.Length; i++)
			{
				int charValue = SymbolTable.IndexOf(code[i]);
				xtConvert.SetBits(bits, i * 7, 7, charValue);
			}
			byte[] arr = new byte[size];
			bits.CopyTo(arr, 0);
			return arr;
		}


		/// <summary>
		/// Decodes the given byte array into a string representing the EANUCC128 bar code.
		/// </summary>
		public static string Decode(byte[] code)
		{
			int NumChars = Convert.ToInt32(Math.Floor((double)code.Length * 8 / 7));
			StringBuilder sb = new StringBuilder(NumChars);
			for (int i = 0; i < NumChars; i++)
			{
				int charIndex = xtConvert.MakeInt32(code, i * 7, 7);
				sb.Append(SymbolTable[charIndex]);
			}
			return sb.ToString();
		}
	}
}
