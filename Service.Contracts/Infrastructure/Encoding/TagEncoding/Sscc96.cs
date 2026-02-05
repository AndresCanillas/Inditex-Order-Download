using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

/* Specification
 * ==========================
 * 
 * Sscc (Serial Shipping Container Code) is used when the company is
 * already registered with the EAN.UCC and whishes to keep using their
 * existing bar codes in their RFID tags.
 * 
 * Internal structure:
 * 
 * | Header | Filter | Partition | CompanyPrefix | SerialReference |RESERVED|
 * |--------|--------|-----------|---------------|-----------------|--------|
 * | 8 bits | 3 bits |   3 bits  |  20-40 bits   |    38-18 bits   |24 bits |
 * 
 * Where:
 * 
 *  - Filter is a value between 0 and 7.
 *  - Partition determines how many bits are used for the CompanyPrefix and SerialReference fields.
 *  - CompanyPrefix is assigned by the EAN.UCC.
 *	- SerialReference is freely assigned by the company.
 *	- Reserved, the use of this area is not defined in the specification, but can contain a 24 bit integer (0-16777215).
 * 
 * URN format
 * ==========================
 * 
 *		"urn:epc:sscc-96:Filter.Company.SerialNumber"
 *
 */

namespace Service.Contracts
{
	public class Sscc96 : TagEncoding, ITagEncoding, IConfigurable<GS1TagConfig>
	{
		private GS1TagConfig config;

		public override int TypeID
		{
			get { return 3; }
		}

		public override string Name
		{
			get { return "Sscc-96"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:sscc-96:Filter.CompanyPrefix.SerialReference"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:sscc-96:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){3}"; }
		}

		public Sscc96()
		{
			config = new GS1TagConfig() { Filter = "1" };

			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",          88, 8,  BitFieldFormat.Decimal, false, true, "49", null),
				new BitFieldInfo("Filter",          85, 3,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Partition",       82, 3,  BitFieldFormat.Decimal, false, false, "0", PartitionChanged),
				new BitFieldInfo("CompanyPrefix",   42, 40, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("SerialReference", 24, 18, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Unallocated",		0,  24, BitFieldFormat.Decimal, false, false, null, null)
			};
		}


		private void PartitionChanged(BitFieldInfo field, byte[] value)
		{
			long v = xtConvert.ByteArrayToInt64(value, 0);
			int company = 40, serial;
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
			serial = 58 - company;
			fields[3].BitLength = company;
			fields[3].StartBit = 24 + serial;
			fields[4].BitLength = serial;
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
