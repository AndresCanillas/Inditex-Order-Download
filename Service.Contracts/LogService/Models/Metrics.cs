using Service.Contracts;

namespace Services.Core
{
	class Metrics
	{
		[PK, Identity]
		public long Id;
		public string SerializedData;

		public Metrics() { }

		public Metrics(string serializedData)
		{
			SerializedData = serializedData;
		}
	}
}
