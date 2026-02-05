using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

/* Specification
 * ==========================
 * 
 * GIAI (Global Individual Asset Identifier)
 * Is used when the company has a record with EAN.UCC and wishes to
 * keep using GIAI codes in their RFID tags.
 * 
 * The internal structure of the code is as follows:
 * 
 * | Header | Filter |Partition| CompanyPrefix |   AssetReference  |
 * |--------|--------|---------|---------------|-------------------|
 * | 8 bits | 3 bits | 3 bits  |  20-40 bits   |    168-126 bits   |
 * 
 * URNs Format
 * ==========================
 * 
 *		"urn:epc:giai-202:Filter.CompanyPrefix.AssetReference"
 *
 */

namespace Service.Contracts
{
	public class Giai202 : TagEncoding, ITagEncoding, IConfigurable<GS1TagConfig>
	{
		private GS1TagConfig config;

		public override int TypeID
		{
			get { return 11; } 
		}

		public override string Name
		{
			get { return "Giai-202"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:giai-202:Filter.CompanyPrefix.AssetReference"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:giai-202:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){3}"; }
		}

		public Giai202()
		{
			config = new GS1TagConfig() { Filter = "1" };

			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",         194, 8,  BitFieldFormat.Decimal,   false, true, "56", null),
				new BitFieldInfo("Filter",         191, 3,  BitFieldFormat.Decimal,   false, false, null, null),
				new BitFieldInfo("Partition",      188, 3,  BitFieldFormat.Decimal,   false, false, "5", PartitionChanged),
				new BitFieldInfo("CompanyPrefix",  148, 40, BitFieldFormat.Decimal,   false, false, null, null),
				new BitFieldInfo("AssetReference", 0,  148, BitFieldFormat.EANUCC128, false, false, null, null),
			};
		}


        private void PartitionChanged(BitFieldInfo field, byte[] value)
		{
			long v = xtConvert.ByteArrayToInt64(value, 0);
			int company = 30, asset;
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
			asset = 82 - company;
			fields[3].BitLength = company;
			fields[3].StartBit = asset;
			fields[4].BitLength = asset;
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
