using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	/* =================================================================================
	 * Encoding that can be used to represent tags that cannot be decoded as any of
	 * the standard encodings.
	 * ================================================================================= 
	 */
	public class UnknownEncoding : TagEncoding
	{
		public static TagEncoding Instance = new UnknownEncoding();

		public override int TypeID
		{
			get { return 999; }
		}

		public override string Name
		{
			get { return "Unknown Encoding"; }
		}

		public override string UrnNameSpace
		{
			get { return "Unknown Encoding"; }
		}

		protected override string UrnPattern
		{
			get { return @""; }
		}

		public UnknownEncoding()
		{
			fields = new BitFieldInfo[0];
		}

		public override string ToString()
		{
			return "Unknown Encoding";
		}
	}
}
