using System;

/* Specification
 * ==========================
 * 
 * GID96 (General Identifier) is a 96 bit code that can be used to identify assets,
 * it has the following internal structure:
 * 
 * |Header|  GMN  |ClassID| Serial |
 * |------|-------|-------|--------|
 * |8 bits|28 bits|24 bits|36 bits |
 * 
 * GMN = General Manager Number
 * 
 * URN Format
 * ==========================
 * 
 *		"urn:epc:gid-96:GMN.ClassID.Serial"
 * 
 */

namespace Service.Contracts
{
	public class Gid96 : TagEncoding, ITagEncoding
	{
		public override int TypeID
		{
			get { return 1; }
		}

		public override string Name
		{
			get { return "Gid-96"; }
		}

		public override string UrnNameSpace
		{
			get { return "urn:epc:gid-96:GMN.ClassID.Serial"; }
		}

		protected override string UrnPattern
		{
			get { return @"^urn:epc:gid-96:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){3}"; }
		}

		public Gid96()
		{
			fields = new BitFieldInfo[]{
				new BitFieldInfo("Header",	88, 8,  BitFieldFormat.Hexadecimal, false, true, "35", null),
				new BitFieldInfo("GMN",		60, 28, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("ClassID",	36, 24, BitFieldFormat.Decimal, false, false, null, null),
				new BitFieldInfo("Serial",	0,  36, BitFieldFormat.Decimal, false,  false, null, null)
			};
		}
	}
}