using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

/* Specification
 * ==========================
 * 
 * GIAI (Global Individual Asset Identifier) is used when the company has
 * been registered with the EAN.UCC and whishes to keep using their existing
 * GIAI codes in their RFID tags.
 * 
 * Giai96 has the following internal structure:
 * 
 * | Header | Filter |Partition| CompanyPrefix |  Serial #  |
 * |--------|--------|---------|---------------|------------|
 * | 8 bits | 3 bits | 3 bits  |  20-40 bits   | 62-42 bits |
 * 
 * 
 * URNs Format
 * ==========================
 * 
 *		"urn:epc:giai-96:Filter.Company.Serial#
 *
 */

namespace Service.Contracts
{
	public class Giai96 : TagEncoding, ITagEncoding, IConfigurable<GS1TagConfig>
	{
		private GS1TagConfig config;

		public override int TypeID
		{
			get { return 6; }
		}

		public override string Name
		{
			get { return "Giai-96"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:giai-96:Filter.CompanyPrefix.AssetReference"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:giai-96:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){3}"; }
		}

		public Giai96()
		{
			config = new GS1TagConfig() { Filter = "1" };

			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",         88, 8,  BitFieldFormat.Decimal, false, true, "52", null),
				new BitFieldInfo("Filter",         85, 3,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Partition",      82, 3,  BitFieldFormat.Decimal, false, false, "5", PartitionChanged),
				new BitFieldInfo("CompanyPrefix",  42, 40, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("AssetReference", 0,  42, BitFieldFormat.Decimal, false, false, null, null),
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
