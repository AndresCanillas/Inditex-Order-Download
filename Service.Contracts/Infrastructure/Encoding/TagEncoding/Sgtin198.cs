using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

/* Specification
 * ==========================
 * 
 * Sgtin198 (Serialized Global Trade Item Number) is used when the company
 * has been registered with the EAN.UCC and whishes to keep using their existing
 * GTIN bar codes in their RFID tags.
 * 
 * Internal structure:
 * 
 * | Header | Filter | Partition | CompanyPrefix | ItemRef | SerialNumber |
 * |--------|--------|-----------|---------------|---------|--------------|
 * | 8 bits | 3 bits |   3 bits  |  20-40 bits   |24-4 bits|   140 bits   |
 * 
 * Where:
 * 
 *  - Filter is a value between 0 and 7
 *  - Partition determines how many bits are used for the CompanyPrefix and the ItemReference fields.
 *  - CompanyPrefix is assigned by the EAN.UCC
 *	- ItemReference is freely assigned by the company to identify a product.
 *	- SerialNumber is a 20 character long code used to assign a unique identifier to each product,
 *	  this is an alphanumeric field using the EAN.UCC128 alphabet.
 * 
 * URN Format
 * ==========================
 * 
 *		"urn:epc:sgtin-198:Filter.CompanyPrefix.ItemReference.SerialNumber"
 */

namespace Service.Contracts
{
	class Sgtin198 : TagEncoding, ITagEncoding, IConfigurable<GS1TagConfig>
	{
		private GS1TagConfig config;

		public override int TypeID
		{
			get { return 8; }
		}

		public override string Name
		{
			get { return "Sgtin-198"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:sgtin-198:Filter.CompanyPrefix.ItemReference.SerialNumber"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:sgtin-198:((?<component>\[\d+-\d+\]|\d+|\*)(\.)){3}(?<component>[!-z]+|\*)$"; }
		}

		public Sgtin198()
		{
			config = new GS1TagConfig() { Filter = "1" };

			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",        190, 8,  BitFieldFormat.Decimal, false, true, "54", null),
				new BitFieldInfo("Filter",        187, 3,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Partition",     184, 3,  BitFieldFormat.Decimal, false, false, "5", PartitionChanged),
				new BitFieldInfo("CompanyPrefix", 154, 30, BitFieldFormat.Decimal, true, false, null, null),
				new BitFieldInfo("ItemReference", 140, 14, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("SerialNumber",  0, 140,  BitFieldFormat.EANUCC128, false, false, null, null)
			};
		}

        private void PartitionChanged(BitFieldInfo field, byte[] value)
		{
			long v = xtConvert.ByteArrayToInt64(value, 0);
			int company = 30, item;
			switch (v)
			{
				case 0: company = 40; break;
				case 1: company = 37; break;
				case 2: company = 34; break;
				case 3: company = 30; break;
				case 4: company = 27; break;
				case 5: company = 24; break;
				case 6: company = 20; break;
			}
			item = 44 - company;
			fields[3].BitLength = company;
			fields[3].StartBit = 140 + item;
			fields[4].BitLength = item;
		}

		
		public GS1TagConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(GS1TagConfig config)
		{
			this.config = config;
			this["Filter"].Value = config.Filter;
		}
	}
}
