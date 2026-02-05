using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Service.Contracts.Infrastructure.Encoding.Tempe
{
	public class EpcApi : IEncodingApi, IConfigurable<EpcApiConfig>
	{
		private EpcApiConfig config;

		public EpcApi()
		{
			config = new EpcApiConfig()
			{
				Model = new VariableDataSource("EPCModel"),
				Quality = new VariableDataSource("EPCQuality"),
				Color = new VariableDataSource("EPCColor"),
				Size = new VariableDataSource("EPCSize"),
				TagType = new VariableDataSource("EPCTagType"),
				TagSubType = new VariableDataSource("EPCTagSubType")
			};
		}

		public EpcApiConfig Config { get => config; }

		public EpcApiConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(EpcApiConfig config)
		{
			this.config = config;
		}
	}

	public class EpcApiConfig
	{
		public IFieldDataSource Model { get; set; }
		public IFieldDataSource Quality { get; set; }
		public IFieldDataSource Color { get; set; }
		public IFieldDataSource Size { get; set; }
		public IFieldDataSource TagType { get; set; }
		public IFieldDataSource TagSubType { get; set; }
	}


	public class PreencodingApi : IEncodingApi, IConfigurable<PreencodingApiConfig>
	{
		private PreencodingApiConfig config;

		public PreencodingApi()
		{
			config = new PreencodingApiConfig()
			{
				BrandId = new FixedDataSource("0"),
				ProductType = new FixedDataSource("0"),
				Color = new FixedDataSource("0"),
				Size = new FixedDataSource("0"),
				TagType = new FixedDataSource("0"),
				TagSubType = new FixedDataSource("0")
			};
		}

		public PreencodingApiConfig Config { get => config; }

		public PreencodingApiConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(PreencodingApiConfig config)
		{
			this.config = config;
		}
	}

	public class PreencodingApiConfig
	{
		public IFieldDataSource BrandId { get; set; }
		public IFieldDataSource ProductType { get; set; }
		public IFieldDataSource Color { get; set; }
		public IFieldDataSource Size { get; set; }
		public IFieldDataSource TagType { get; set; }
		public IFieldDataSource TagSubType { get; set; }
	}
}
