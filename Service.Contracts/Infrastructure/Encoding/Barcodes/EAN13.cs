using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	/*
	 * Also known as GTIN-13, is a 13 digit code composed of:
	 *	> N digit company code (number of digits varies depending on GS1 prefix table)
	 *	> 12-N digit product code (number of digits varies depending on GS1 prefix table)
	 *	> 1 check digit
	 *	
	 *	NOTES:
	 *		- The country code (first two digits) is interpreted as part of the company prefix when encoding a tag.
	 *		- Check digit is calculated using the standard MOD algorithm from GS1.
	 *		- For GTIN-13 we are assuming that the ItemReference will always start with a 0
	 *		
	 *	If a 13 digit code is supplied, the last digit is interpreted as the check digit.
	 *	If a 12 digit code is supplied, we assume that the check digit is missing, and we calculate it ourselves.
	 */
	public class EAN13 : Barcode1D, IBarcode1D, IConfigurable<EmptyConfig>
	{
		private string companyCode;
		private string productCode;

		public override string Code
		{
			get
			{
				return base.Code;
			}
			set
			{
				if (value == null)
					throw new InvalidOperationException("Cannot set EAN13.Code to null");
				if (!value.IsNumeric() || value.Length < 12 || value.Length > 13)
					throw new InvalidOperationException($"Value {value} is not a valid EAN13.");
				if (value.Length == 12)
				{
					value += GetCheckDigit(value);
				}
				else
				{
					var checkDigit = GetCheckDigit(value.Substring(0, 12));
					if (checkDigit != value[12])
						throw new InvalidOperationException($"Invalid EAN13 ({value}): Check Digit is invalid.");
				}
				base.Code = value;
			}
		}

		public List<GS1CompanyPrefix> GS1CompanyPrefixOverrides { get; set; }

		public override void Encode(ITagEncoding tag, string code)
		{
			if (!tag.ContainsField("Partition") || !tag.ContainsField("CompanyPrefix") || !tag.ContainsField("ItemReference"))
				throw new InvalidOperationException($"Invalid TagEncoding Scheme {tag.Name}. EAN-13 barcode requires a TagEncoding Scheme that supports fields 'Partition', 'CompanyPrefix' and 'ItemReference'.");
			if (code.Length < 12 || code.Length > 13)
				throw new InvalidOperationException($"Invalid EAN-13 code: '{code}'. Expected either a 12 or 13 digit code.");
			Code = code;
			var partition = GS1Prefixes.GetPartition(code, GS1CompanyPrefixOverrides);
			var gcpLength = 12 - partition;
			var itemLength = 12 - gcpLength;
			companyCode = Code.Substring(0, gcpLength);
			productCode = Code.Substring(gcpLength, itemLength);
			tag["Partition"].Value = partition.ToString();
			tag["CompanyPrefix"].Value = companyCode;
			tag["ItemReference"].Value = productCode;
		}

		public override void CodeUpdated(string code)
		{
			if (String.IsNullOrWhiteSpace(code))
			{
				companyCode = "";
				productCode = "";
			}
			else
			{
				if (code.Length < 12 || code.Length > 13)
					throw new InvalidOperationException("The specified code is not a valid EAN13");
				companyCode = code.Substring(0, 7);
				productCode = code.Substring(7, 5);
			}
		}

		public string CompanyCode { get { return companyCode; } }
		public string ProductCode { get { return productCode; } }

		public EmptyConfig GetConfiguration()
		{
			return EmptyConfig.Value;
		}

		public void SetConfiguration(EmptyConfig config)
		{
		}
	}
}
 