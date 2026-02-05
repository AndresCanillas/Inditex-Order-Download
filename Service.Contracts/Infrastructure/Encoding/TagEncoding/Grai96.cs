using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

/* Specification
 * ==========================
 * 
 * GRAI (Global Returnable Asset Identifier) is used when the company is
 * registered with the EAN.UCC and whishes to keep using their existing 
 * GRAI bar codes in their RFID tags.
 * 
 * A Grai96 code has the following internal structure:
 * 
 * | Header | Filter | Partition | CompanyPrefix |Asset Type | #Serial |
 * |--------|--------|-----------|---------------|-----------|---------|
 * | 8 bits | 3 bits |   3 bits  |  20-40 bits   | 24-4 bits | 38 bits |
 * 
 * 
 * URN Format
 * ==========================
 * 
 *		"urn:epc:grai-96:Filter.Company.AssetType.Serial#
 *
 */

namespace Service.Contracts
{
	public class Grai96 : TagEncoding, ITagEncoding, IConfigurable<GS1TagConfig>
	{
		private GS1TagConfig config;

		public override int TypeID
		{
			get { return 5; }
		}

		public override string Name
		{
			get { return "Grai-96"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:grai-96:Filter.CompanyPrefix.AssetType.SerialNumber"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:grai-96:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){4}"; }
		}

		public Grai96()
		{
			config = new GS1TagConfig() { Filter = "1" };

			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",        88, 8,  BitFieldFormat.Decimal, false, true, "51", null),
				new BitFieldInfo("Filter",        85, 3,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Partition",     82, 3,  BitFieldFormat.Decimal, false, false, "3", PartitionChanged),
				new BitFieldInfo("CompanyPrefix", 52, 30, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("AssetType",     38, 14, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("SerialNumber",  0,  38, BitFieldFormat.Decimal, false, false, null, null)
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
			asset = 44 - company;
			fields[3].BitLength = company;
			fields[3].StartBit = 38 + asset;
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
