using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zebra.Sdk.Util.Internal
{
	internal class Extension
	{
		public static Extension TXT;

		public static Extension CSV;

		public static Extension BAS;

		public static Extension BAE;

		public static Extension HTM;

		public static Extension WML;

		public static Extension EPL;

		public static Extension PAC;

		public static Extension NRD;

		public static Extension TTE;

		public static Extension TTF;

		public static Extension ASC;

		public static Extension IMG;

		public static Extension BMP;

		public static Extension PCX;

		public static Extension ZPL;

		public static Extension DBC;

		public static Extension PNG;

		public static Extension GRF;

		public static Extension KEY;

		public static Extension FNT;

		private static List<Extension> extensions;

		private string extension;

		private ObjectType type;

		static Extension()
		{
			Extension.TXT = new Extension("TXT", ObjectType.TXTobj);
			Extension.CSV = new Extension("CSV", ObjectType.CSVobj);
			Extension.BAS = new Extension("BAS", ObjectType.BASICobj);
			Extension.BAE = new Extension("BAE", ObjectType.BAEobj);
			Extension.HTM = new Extension("HTM", ObjectType.HTMobj);
			Extension.WML = new Extension("WML", ObjectType.WMLobj);
			Extension.EPL = new Extension("EPL", ObjectType.EFORMATobj);
			Extension.PAC = new Extension("PAC", ObjectType.PAC_FASTobj);
			Extension.NRD = new Extension("NRD", ObjectType.NRD_TLSobj);
			Extension.TTE = new Extension("TTE", ObjectType.TTEobj);
			Extension.TTF = new Extension("TTF", ObjectType.TTFobj);
			Extension.ASC = new Extension("ASC", ObjectType.ASCobj);
			Extension.IMG = new Extension("IMG", ObjectType.IMGobj);
			Extension.BMP = new Extension("BMP", ObjectType.BMPobj);
			Extension.PCX = new Extension("PCX", ObjectType.PCXobj);
			Extension.ZPL = new Extension("ZPL", ObjectType.FORMATobj);
			Extension.DBC = new Extension("DBC", ObjectType.DBCobj);
			Extension.PNG = new Extension("PNG", ObjectType.PIMAGEobj);
			Extension.GRF = new Extension("GRF", ObjectType.GIMAGEobj);
			Extension.KEY = new Extension("KEY", ObjectType.KEYobj);
			Extension.FNT = new Extension("FNT", ObjectType.FONTobj);
			Extension.extensions = new List<Extension>()
			{
				Extension.TXT,
				Extension.CSV,
				Extension.BAS,
				Extension.BAE,
				Extension.HTM,
				Extension.WML,
				Extension.EPL,
				Extension.PAC,
				Extension.NRD,
				Extension.TTE,
				Extension.TTF,
				Extension.ASC,
				Extension.IMG,
				Extension.BMP,
				Extension.PCX,
				Extension.ZPL,
				Extension.DBC,
				Extension.PNG,
				Extension.GRF,
				Extension.KEY,
				Extension.FNT
			};
		}

		private Extension(string extension, ObjectType type)
		{
			this.extension = extension;
			this.type = type;
		}

		private string GetExtension()
		{
			return this.extension;
		}

		public new ObjectType GetType()
		{
			return this.type;
		}

		public static int GetTypeValue(string extension)
		{
			int type;
			List<Extension>.Enumerator enumerator = Extension.extensions.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					Extension current = enumerator.Current;
					if (!Regex.IsMatch(current.GetExtension(), extension, RegexOptions.IgnoreCase))
					{
						continue;
					}
					type = (int)current.GetType();
					return type;
				}
				return 8;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
		}
	}
}