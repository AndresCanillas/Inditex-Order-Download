using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

/*
 * Specification
 * ==========================
 * 
 * The SGTIN-96 encoding  (Serialized Global Trade Item Number) is used when 
 * the company is already registered with the EAN.UCC and whishes to keep
 * using their GTIN bar codes in their RFID tags. 
 * 
 * The structure of the SGTIN-96 code is as follows:
 * 
 * | Header | Filter | Partition | CompanyPrefix | ItemRef | #Serial |
 * |--------|--------|-----------|---------------|---------|---------|
 * | 8 bits | 3 bits |   3 bits  |  20-40 bits   |24-4 bits| 38 bits |
 * 
 * 
 * Where:
 * 
 *	- Header is a fixed value (always equal to 48)
 *  - Filter must be a value between 0 and 7.
 *  - Partition controls the size of the CompanyPrefix and ItemReference fields.
 *  - CompanyPrefix is the prefix assigned to the company by the EAN.UCC (must be provided to us by the company).
 *	- ItemReference is the number assigned by the company to the product, and
 *	- SerialNumber is a "unique" identifier assigned by the company to be able to track each item individually (it is
 *	  assumed that each item will have its own unique serial number). How and when this serial number is initialized
 *	  and incremented depends on the company policies. For instance, each ItemReference code can have its own independent
 *	  serial sequence, or to simplify things, the company might decide to keep a single sequence of serials that is used
 *	  across all products regardless of the ItemReferece.
 *	  
 *	  
 * URNs format
 * ==========================
 * URN format for this standard is defined below:
 * 
 *		"urn:epc:sgtin-96:Filter.CompanyPrefix.ItemReference.SerialNumber"
 *
 * 
 */

namespace Service.Contracts
{
	public class Sgtin96 : TagEncoding, ITagEncoding, IConfigurable<GS1TagConfig>
	{
		private GS1TagConfig config;

		public override int TypeID
		{
			get { return 2; }
		}

		public override string Name
		{
			get { return "SGTIN-96"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:tag:sgtin-96:Filter.CompanyPrefix.ItemReference.SerialNumber"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:tag:sgtin-96:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){4}"; }
		}

		public Sgtin96()
		{
			config = new GS1TagConfig() { Filter = "1" };

			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",		  88, 8,  BitFieldFormat.Decimal, false, true,  "48", null),
				new BitFieldInfo("Filter",        85, 3,  BitFieldFormat.Decimal, false, false, "1", null),
				new BitFieldInfo("Partition",     82, 3,  BitFieldFormat.Decimal, false, false, "3", PartitionChanged),
				new BitFieldInfo("CompanyPrefix", 52, 30, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("ItemReference", 38, 14, BitFieldFormat.Decimal, true, false, null, null),
				new BitFieldInfo("SerialNumber",  0,  38, BitFieldFormat.Decimal, false, false, null, null)
			};
		}


		private void PartitionChanged(BitFieldInfo field, byte[] value)
		{
			long v = xtConvert.ByteArrayToInt64(value, 0);
			int companyLength = 30, itemReferenceLength;
			switch (v)
			{
				case 0: companyLength = 40; break;
				case 1: companyLength = 37; break;
				case 2: companyLength = 34; break;
				case 3: companyLength = 30; break;
				case 4: companyLength = 27; break;
				case 5: companyLength = 24; break;
				case 6: companyLength = 20; break;
			}
			itemReferenceLength = 44 - companyLength;
			fields[3].BitLength = companyLength;
			fields[3].StartBit = 38 + itemReferenceLength;
			fields[4].BitLength = itemReferenceLength;
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

		public override string ToString()
		{
			return $"sgtin-96: {this["CompanyPrefix"]}.{this["ItemReference"]}.{this["SerialNumber"]}";
		}
	}
}
