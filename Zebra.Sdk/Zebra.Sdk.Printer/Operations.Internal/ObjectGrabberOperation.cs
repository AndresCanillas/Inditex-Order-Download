using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class ObjectGrabberOperation : PrinterOperationCaresAboutLinkOsVersion<Stream>
	{
		protected long MAX_INTER_CHARACTER_DELAY_TIME = (long)15000;

		private string fullObjectPath;

		public ObjectGrabberOperation(string fullObjectPath, Connection connection, PrinterLanguage language, LinkOsInformation linkOsInformation) : base(connection, language, linkOsInformation)
		{
			if (fullObjectPath == null || 2 >= fullObjectPath.Length)
			{
				throw new ArgumentException("File name not provided");
			}
			if (fullObjectPath[1] != ':')
			{
				throw new ArgumentException("Drive letter not specified");
			}
			this.fullObjectPath = fullObjectPath;
		}

		public override Stream Execute()
		{
			this.SelectProperChannel();
			this.IsOkToProceed();
			return this.RetrieveStreamToObject();
		}

		private void IsOkToProceed()
		{
			if (!base.IsLinkOs2_5_OrHigher() && (this.connection is StatusConnection || !this.connection.Connected))
			{
				throw new ConnectionException("Cannot retrieve objects over status channel on this version of firmware");
			}
			if (base.IsPrintingChannelInLineMode())
			{
				throw new ConnectionException("Cannot retrieve objects from printer over printing channel when in line mode");
			}
		}

		private Stream RetrieveStreamToObject()
		{
			Stream multipartFormReceiverStream = null;
			if (this.ShouldRequestMultipartForm())
			{
				MultipartFileRequester.Send(this.connection, this.fullObjectPath);
				multipartFormReceiverStream = new MultipartFormReceiverStream(this.connection, this.MAX_INTER_CHARACTER_DELAY_TIME);
			}
			else if (FileWrapper.IsHzoExtension(this.fullObjectPath.Substring(this.fullObjectPath.LastIndexOf('.') + 1)))
			{
				try
				{
					this.connection.Write(Encoding.UTF8.GetBytes(ZPLUtilities.GetHZO(this.fullObjectPath)));
				}
				catch (ZebraIllegalArgumentException zebraIllegalArgumentException1)
				{
					ZebraIllegalArgumentException zebraIllegalArgumentException = zebraIllegalArgumentException1;
					throw new ConnectionException(zebraIllegalArgumentException.Message, zebraIllegalArgumentException);
				}
				multipartFormReceiverStream = new PrinterConnectionInputStream(this.connection, this.MAX_INTER_CHARACTER_DELAY_TIME, "</ZEBRA-ELTRON-PERSONALITY>\r\n");
			}
			return multipartFormReceiverStream;
		}

		private bool ShouldRequestMultipartForm()
		{
			if (!base.IsLinkOs2_5_OrHigher())
			{
				return false;
			}
			if (this.printerLanguage != PrinterLanguage.LINE_PRINT)
			{
				return true;
			}
			return this.connection is StatusConnection;
		}
	}
}