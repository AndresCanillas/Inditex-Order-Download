using Newtonsoft.Json;
using System;

namespace Zebra.Sdk.Util.Internal
{
	[JsonObject]
	internal class CertificateSigningRequestInfo
	{
		private string organizationName;

		private string organizationUnitName;

		private string commonName;

		private string companyStreetAddress;

		private string companyCity;

		private string companyState;

		private string companyCountry;

		private string companyEmailAddress;

		private string uid;

		private string companyPostalCode;

		private string companyPhoneNumber;

		private string csr;

		[JsonProperty(PropertyName="commonName")]
		public string CommonName
		{
			get
			{
				return this.commonName;
			}
			set
			{
				this.commonName = value;
			}
		}

		[JsonProperty(PropertyName="companyCity")]
		public string CompanyCity
		{
			get
			{
				return this.companyCity;
			}
			set
			{
				this.companyCity = value;
			}
		}

		[JsonProperty(PropertyName="companyCountry")]
		public string CompanyCountry
		{
			get
			{
				return this.companyCountry;
			}
			set
			{
				this.companyCountry = value;
			}
		}

		[JsonProperty(PropertyName="companyEmailAddress")]
		public string CompanyEmailAddress
		{
			get
			{
				return this.companyEmailAddress;
			}
			set
			{
				this.companyEmailAddress = value;
			}
		}

		[JsonProperty(PropertyName="companyPhoneNumber")]
		public string CompanyPhoneNumber
		{
			get
			{
				return this.companyPhoneNumber;
			}
			set
			{
				this.companyPhoneNumber = value;
			}
		}

		[JsonProperty(PropertyName="companyPostalCode")]
		public string CompanyPostalCode
		{
			get
			{
				return this.companyPostalCode;
			}
			set
			{
				this.companyPostalCode = value;
			}
		}

		[JsonProperty(PropertyName="companyState")]
		public string CompanyState
		{
			get
			{
				return this.companyState;
			}
			set
			{
				this.companyState = value;
			}
		}

		[JsonProperty(PropertyName="companyStreetAddress")]
		public string CompanyStreetAddress
		{
			get
			{
				return this.companyStreetAddress;
			}
			set
			{
				this.companyStreetAddress = value;
			}
		}

		[JsonProperty(PropertyName="csr")]
		public string Csr
		{
			get
			{
				return this.csr;
			}
			set
			{
				this.csr = value;
			}
		}

		[JsonProperty(PropertyName="organizationName")]
		public string OrganizationName
		{
			get
			{
				return this.organizationName;
			}
			set
			{
				this.organizationName = value;
			}
		}

		[JsonProperty(PropertyName="organizationUnitName")]
		public string OrganizationUnitName
		{
			get
			{
				return this.organizationUnitName;
			}
			set
			{
				this.organizationUnitName = value;
			}
		}

		[JsonProperty(PropertyName="uid")]
		public string Uid
		{
			get
			{
				return this.uid;
			}
			set
			{
				this.uid = value;
			}
		}

		public CertificateSigningRequestInfo()
		{
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}