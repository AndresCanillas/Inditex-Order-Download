using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

/* Specification
 * ==========================
 * 
 * The USDoD96 (US Department of Defense) codification standard is
 * used when the company has a CAGE number (Commercial and Government
 * Entity) and has been instructed to use this encoding scheme.
 * 
 * The internal structure of a USDoD96 code is as follows:
 * 
 * | Header | Filter |   GMI   | Serial # |
 * |--------|--------|---------|----------|
 * | 8 bits | 4 bits |48 bits  | 36 bits  |
 * 
 * 
 * URNs format
 * ==========================
 * 
 *		"urn:epc:usdod-96:Filter.GMI.Serial#
 *
 */

namespace Service.Contracts
{
	public class DoD96 : TagEncoding, ITagEncoding, IConfigurable<GS1TagConfig>
	{
		private GS1TagConfig config;

		public override int TypeID
		{
			get { return 7; }
		}

		public override string Name
		{
			get { return "DoD-96"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:dod-96:Filter.GMI.SerialNumber"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:dod-96:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){3}"; }
		}

		public DoD96()
		{
			config = new GS1TagConfig() { Filter = "1" };

			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",		 88, 8,  BitFieldFormat.Decimal, false, true, "47", null),
				new BitFieldInfo("Filter",		 84, 4,  BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("GMI",			 36, 48, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("SerialNumber", 0, 36,  BitFieldFormat.Decimal, false, false, null, null)
			};
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
