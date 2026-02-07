using System;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class SendJsonOperation : PrinterOperationBase<string>
	{
		private string jsonDataToSend;

		public SendJsonOperation(Connection connection, PrinterLanguage language, string jsonRequest) : base(connection, language)
		{
			this.jsonDataToSend = jsonRequest;
		}

		public override string Execute()
		{
			if (!JsonHelper.IsValidJson(this.jsonDataToSend))
			{
				throw new ConnectionException("Invalid JSON request.");
			}
			this.SelectStatusChannelIfOpen();
			if (base.IsPrintingChannelInLineMode())
			{
				throw new ConnectionException("Cannot send JSON over raw port when printer is in line print mode.");
			}
			return Encoding.UTF8.GetString(this.connection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes(this.jsonDataToSend), this.connection.MaxTimeoutForRead, this.connection.TimeToWaitForMoreData, new JsonValidator()));
		}
	}
}