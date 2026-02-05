using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	public class AllocateSerials : ITagEncodingProcess, IAllocateSerialsProcess, IConfigurable<AllocateSerialsConfig>
	{
		private AllocateSerialsConfig config;

		public AllocateSerials(IFactory factory)
		{
			config = new AllocateSerialsConfig()
			{
				Algorithm = new StandardEncodingAlgorithm(factory)
			};
		}

		public ITagEncodingAlgorithm Algorithm { get => config.Algorithm; }

		public bool IsSerialized { get => Algorithm.IsSerialized; }

		public List<TagEncodingInfo> Encode(EncodeRequest request)
		{
			return config.Algorithm.Encode(request);
		}

		public string EncodeHeader(JObject data, int count, long startingSerial, int tagsPerSheet = 1)
		{
			return config.Algorithm.EncodeHeader(data, count, startingSerial, tagsPerSheet);
		}

		public TagEncodingInfo EncodeSample(JObject data)
		{
			return config.Algorithm.EncodeSample(data);
		}

		public AllocateSerialsConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(AllocateSerialsConfig config)
		{
			this.config = config;
		}
	}

	public class AllocateSerialsConfig
	{
		public ITagEncodingAlgorithm Algorithm;
	}
}
