using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

/* Specification
 * ==========================
 * 
 * Sgln96 (Global Location Number) is used when the company
 * has been registered with the EAN.UCC and whishes to keep using their
 * existing GLN bar codes in their RFID tags. 
 * 
 * Internal structure:
 * 
 * | Header | Filter | Partition | CompanyPrefix |LocationRef| Extension |
 * |--------|--------|-----------|---------------|-----------|-----------|
 * | 8 bits | 3 bits |   3 bits  |  20-40 bits   | 21-1 bits |   41 bits |
 * 
 * 
 * Where:
 * 
 *  - Filter is a value between 0 and 7.
 *  - Partition determines how many bits are used for the CompanyPrefix and the LocationReference fields.
 *  - CompanyPrefix is assigned by EAN.UCC.
 *	- LocationReference is assigned freely by the company and allows to identify a physical location.
 *  - Extension, its content is not currently defined by the standard, but can be freely assigned by the company, in this case
 *    we can store a 41 bit integer (ranging from 0 to 16777215).
 * 
 * URN format
 * ==========================
 * 
 *		"urn:epc:sgln-96:Filter.Company.LocationReference.Extension"
 *
 */

namespace Service.Contracts
{
	public class Sgln96 : TagEncoding, ITagEncoding, IConfigurable<GS1TagConfig>
	{
		private GS1TagConfig config;

		public override int TypeID
		{
			get { return 4; }
		}

		public override string Name
		{
			get { return "Sgln-96"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:sgln-96:Filter.CompanyPrefix.LocationReference"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:sgln-96:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){3}"; }
		}

		public Sgln96()
		{
			config = new GS1TagConfig() { Filter = "1" };

			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",				88, 8,  BitFieldFormat.Decimal, false, true, "50", null),
				new BitFieldInfo("Filter",				85, 3,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Partition",			82, 3,  BitFieldFormat.Decimal, false, false, "3", PartitionChanged),
				new BitFieldInfo("CompanyPrefix",		52, 30, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("LocationReference",	41, 11, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Extension",			0,  41, BitFieldFormat.Decimal, false, false, null, null)
			};
		}


        private void PartitionChanged(BitFieldInfo field, byte[] value)
		{
			long v = xtConvert.ByteArrayToInt64(value, 0);
			int company = 30, location;
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
			location = 41 - company;
			fields[3].BitLength = company;
			fields[3].StartBit = 41 + location;
			fields[4].BitLength = location;
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
