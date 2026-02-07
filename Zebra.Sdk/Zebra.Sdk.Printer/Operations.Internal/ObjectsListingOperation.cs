using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class ObjectsListingOperation : PrinterOperationCaresAboutLinkOsVersion<List<PrinterObjectProperties>>
	{
		private HashSet<DriveType> driveTypes;

		public ObjectsListingOperation(Connection connection, PrinterLanguage language, LinkOsInformation linkOsInformation) : this(connection, language, linkOsInformation, new HashSet<DriveType>())
		{
		}

		public ObjectsListingOperation(Connection connection, PrinterLanguage language, LinkOsInformation linkOsInformation, HashSet<DriveType> driveTypes) : base(connection, language, linkOsInformation)
		{
			this.driveTypes = driveTypes;
		}

		public override List<PrinterObjectProperties> Execute()
		{
			this.SelectProperChannel();
			this.IsOkToProceed();
			return this.RetrieveObjectListing();
		}

		private void IsOkToProceed()
		{
			if (!base.IsLinkOs2_5_OrHigher())
			{
				if (this.connection is StatusConnection)
				{
					throw new ConnectionException("Cannot retrieve object listing over the status channel on this version of firmware");
				}
				if (this.printerLanguage == PrinterLanguage.LINE_PRINT)
				{
					throw new ConnectionException("Cannot retrieve object listing when in line print mode on this version of firmware");
				}
			}
		}

		private List<PrinterObjectProperties> RetrieveObjectListing()
		{
			List<PrinterObjectProperties> printerObjectProperties;
			try
			{
				List<StorageInfo> storageInfos = new List<StorageInfo>();
				if (this.driveTypes != null && this.driveTypes.Count > 0)
				{
					storageInfos = (new StorageInfoGrabber(this.connection, this.printerLanguage, this.linkOsInformation)).Execute();
				}
				if (!this.ShouldRetrieveViaFileListing())
				{
					printerObjectProperties = (!base.IsLinkOs2_5_OrHigher() ? (new FileUtilZpl(this.connection)).RetrieveObjectsProperties(storageInfos, this.driveTypes) : (new FileUtilZpl(this.connection)).RetrieveObjectsPropertiesWithCrc32(storageInfos, this.driveTypes, this.printerLanguage));
				}
				else
				{
					printerObjectProperties = (new FileUtilZpl(this.connection)).RetrieveObjectsPropertiesWithCrc32(storageInfos, this.driveTypes, this.printerLanguage);
				}
			}
			catch (Exception exception)
			{
				throw new ConnectionException(exception.Message);
			}
			return printerObjectProperties;
		}

		private bool ShouldRetrieveViaFileListing()
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