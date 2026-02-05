using System;
using System.Collections.Generic;
using System.Text;


/*
 * Specification
 * ==========================
 * 
 * This is a customer defined encoding scheme used exclusively for brands and companies
 * afiliated to Inditex Spain. 
 * 
 * Internal Structure
 * 
 * | Version | Brand  | Section |ProdType|  MCCT  | ActFlag  | WCheck | SerialNumber |  WDate   | ReuseCount | VersionC |   ProvID  |  UNUSED  | EASFlag |PaperTag |
 * |---------|--------|---------|--------|--------|----------|--------|--------------|----------|------------|----------|-----------|----------|---------|---------|
 * | 5 bits  | 6 bits |  2 bits | 4 bits | 40 bits|  1 bits  | 6 bits |    32 bits   |  11 bits |   6 bits   |  5 bits  |   5 bits  |  3 bits  |  1 bit  |  1 bit  |
 * 
 * 
 * Where:
 * 
 *	- Version is a 5 bit integer that MUST be set to 1 (as of this writtting). It is used to identify the version of the encoding used in the tag.
 *	
 *	- Brand is a 6 bit integer
 *	
 *	- Section is a 2 bit integer
 *	
 *	- ProdType (Product Type) is a 4 bit integer, right now it only has two defined values: 0 = Cloths, 1 = Shooes
 *	
 *	- MCCT stands for spanish "Modelo/Calidad/Color/Talla", this field is a 40 bit integer value. The value can be calculated by:
 *		1) Each individual field (Modelo/Calidad/Color/Talla) must be an integer value.
 *		2) Multiply Modelo by 100000000
 *		3) Multiply Calidad by 100000
 *		4) Multiply Color by 100
 *		5) Then add together Modelo + Calidad + Color + Talla to obtain the final value.
 *		6) Take the 40 less significative bits from the value calculated above and encode that in the MCCT field. 
 *		
 *		An alternate algorithm using string conversion and concatenation (not recomended for performance):
 *		1) Convert each number to a string.
 *		2) Ensure Modelo is 4 characters long at most, Calidad is 3 characters long at most, Color is 3 characters long at most, and Talla is 2 characters long at most.
 *		3) Add padding zeros to the left of each of the fields as necesary to ensure they are the exact number of characters specified above.
 *		4) Concatenate the strings together in this order: Modelo+Calidad+Color+Talla
 *		5) Convert the resulting string to an Int64
 *		6) Take the 40 less significative bits and encode that in the MCCT field. 
 *		
 *	- ActFlag stands for "Active Flag", 0 = Tag no activo (wounded), 1 = Tag is Active (Unwounded)
 *	
 *	- WCheck stands for "Write Check", these bits can be left set to zero.
 *	
 *	- SerialNumber is a 32 bit interger
 *	
 *	- WDate stands for "Write Date", this must contain the 2 digit month in the most significative digits, and the tow digit year in the least significative digits.
 *	  Example, for a date such as 07/2012:
 *		- Take the month (7 in this example), multiply it for 100, then add the 2 digit year (12 in this example) to get: 01011001000 binary or 712 in decimal.
 *	
 *	- ReuseCount is a 6 bit integer. Can be left in zero.
 *	
 *	- VersionC is a 5 bit integer, this field is just a copy of the version set previously, set to 1 as well.
 *	
 *	- ProvID stands for "Provider ID", it is a 5 bit integer that identifies the company that produces the tag. In our case this field must always be 4 = INDET.
 *	
 *	- UNUSED set these 3 bits to zero
 *	
 *	- EASFlag, 0 = Tag with EAS, 1 = Tag without EAS
 *	
 *	- TagType, 0 = Tag with hard alarm, 1 = Cardboard/Paper Tag
 *	  
 *	  
 * URN format
 * ==========================
 * 
 *  In this case the URN is not standards based, it simply allows to quickly see the most important information in the tag at a glance.
 *  
 *		"urn:tempe:v1:Brand.PType.MCCT.SerialNumber"
 *		
 * Example:
 *		"urn:tempe:v1:1.0.122100102002.2157865"
 */

namespace Service.Contracts
{
	public class Tempe128 : TagEncoding, ITagEncoding
	{
		public override int TypeID
		{
			get { return 12; }
		}

		public override string Name
		{
			get { return "TEMPEv1"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:tempe:v1:Brand.ProdType.MCCT.SerialNumber"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:tempe:v1:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){4}"; }
		}

		public Tempe128()
		{
			fields = new BitFieldInfo[]{
				new BitFieldInfo("Version",			123, 5,  BitFieldFormat.Decimal, false, true, "1", null),
				new BitFieldInfo("Brand",			117, 6,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Section",			115, 2,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("ProdType",		111, 4,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("MCCT",			71,  40, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("ActFlag",			70,  1,  BitFieldFormat.Decimal, false, false, "1", null),
				new BitFieldInfo("WCheck",			64,  6,  BitFieldFormat.Decimal, false, false, "0", null),
				new BitFieldInfo("SerialNumber",	32,  32, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("WDate",			21,  11, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("ReuseCount",		15,  6,  BitFieldFormat.Decimal, false, false, "0", null),
				new BitFieldInfo("VersionC",		10,  5,  BitFieldFormat.Decimal, false, true, "1", null),
				new BitFieldInfo("ProvID",			5,   5,  BitFieldFormat.Decimal, false, false, "4", null),
				new BitFieldInfo("UNUSED",			2,   3,  BitFieldFormat.Decimal, false, false, "0", null),
				new BitFieldInfo("EASFlag",			1,   1,  BitFieldFormat.Decimal, false, false, "1", null),
				new BitFieldInfo("PaperTag",		0,   1,  BitFieldFormat.Decimal, false, false, "1", null)
			};
		}


		public void SetWriteDate(DateTime date)
		{
			int v = (date.Month * 100) + (date.Year % 100);
			fields[8].Value = v.ToString();
		}

		public string GetWriteDate()
		{
			int v = Int32.Parse(fields[8].Value);
			int month = v / 100;
			int year = v % 100;
			return month.ToString("D2") + "/" + year.ToString("D2");
		}

		public void SetMCCT(int model, int quality, int color, int size)
		{
			long v = ((long)(model % 10000)) * 100000000 + ((long)(quality % 1000)) * 100000 + ((long)(color % 1000)) * 100 + (size % 100);
			fields[4].Value = v.ToString();
		}

		public string GetMCCT()
		{
			long v = Int64.Parse(fields[4].Value);
			long model = v / 100000000;
			long quality = (v / 100000) % 1000;
			long color = (v / 100) % 1000;
			long size = v % 100;
			return $"{model.ToString("D4")}/{quality.ToString("D3")}/{color.ToString("D3")}/{size.ToString("D2")}";
		}

		public override string ToString()
		{
			return $"{Name}:{this["Section"]}.{this["Brand"]}.{this["ProdType"]} {GetMCCT()} {this["SerialNumber"]}";
		}
	}
}
