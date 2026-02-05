using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Service.Contracts;
using Service.Contracts.Infrastructure.Encoding.Tempe;

namespace Service.Contracts
{
	public class RFIDConfigurationSystem : IConfigurationSystem<RFIDConfigurationInfo>
	{
		private IFactory factory;

		public string Name { get { return "rfid"; } }

		public void Setup(IFactory factory, IConfigurationContext ctx)
		{
			this.factory = factory;
			ctx.RegisterComponent<IFieldDataSource>();
			ctx.RegisterComponent<IPasswordDeriveMethod>();
			ctx.RegisterComponent<IBarcode1D>();
			ctx.RegisterComponent<ISerialSequence>();
			ctx.RegisterComponent<ITagEncoding>();
			ctx.RegisterComponent<IUserMemoryMethod>();
			ctx.RegisterComponent<ITagEncodingProcess>();
			ctx.RegisterComponent<ITagEncodingAlgorithm>();
			ctx.RegisterComponent<IEncodingApi>();
		}

		public RFIDConfigurationInfo GetConfiguration()
		{
			return new RFIDConfigurationInfo()
			{
				Process = new AllocateSerials(factory)
			};
		}
	}

	public class RFIDConfigurationInfo
	{
		public ITagEncodingProcess Process;
	}
}
