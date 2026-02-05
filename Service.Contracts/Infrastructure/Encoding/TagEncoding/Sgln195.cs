using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

/* Specification
 * ==========================
 * 
 * Sgln195 (Global Location Number) is used when the company has
 * been registered with the EAN.UCC and whishes to keep using their existing
 * GLN barcodes in their RFID tags. 
 * 
 * Internal structure:
 * 
 * | Header | Filter | Partition | CompanyPrefix |LocationRef| Extension |
 * |--------|--------|-----------|---------------|-----------|-----------|
 * | 8 bits | 3 bits |   3 bits  |  20-40 bits   | 21-1 bits | 140 bits  |
 * 
 * 
 * Where:
 * 
 *  - Filter is a value between 0 and 7.
 *  - Partition determines how many bits are used for the CompanyPrefix and the LocationReference fields.
 *  - CompanyPrefix is assigned by EAN.UCC.
 *	- LocationReference is assigned freely by the company and allows to identify a physical location.
 *  - Extension is freely assigned by the company and its meaning is application dependant, in this case
 *    it is a 20 char long alfanumeric field using the EANUCC128 alphabet (7 bits per character).
 * 
 * URN Format
 * ==========================
 * 
 *		"urn:epc:sgln-195:Filter.Company.LocationReference.Extension"
 *
 */

namespace Service.Contracts
{
	public class Sgln195 : TagEncoding, ITagEncoding, IConfigurable<GS1TagConfig>
	{
		private GS1TagConfig config;

		public override int TypeID
		{
			get { return 9; }
		}

		public override string Name
		{
			get { return "Sgln-195"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:sgln-195:Filter.CompanyPrefix.LocationReference.Extension"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:sgln-195:((?<component>\[\d+-\d+\]|\d+|\*)\.){3}(?<component>[!-z]+|\*)$"; }
		}

		public Sgln195()
		{
			config = new GS1TagConfig() { Filter = "1" };

			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",            187, 8,  BitFieldFormat.Decimal, false, true, "57", null),
				new BitFieldInfo("Filter",            184, 3,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Partition",         181, 3,  BitFieldFormat.Decimal, false, false, "3", PartitionChanged),
				new BitFieldInfo("CompanyPrefix",     151, 30, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("LocationReference", 140, 11, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Extension",         0, 140,  BitFieldFormat.EANUCC128, false, false, "", null)
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
			fields[3].StartBit = 140 + location;
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
