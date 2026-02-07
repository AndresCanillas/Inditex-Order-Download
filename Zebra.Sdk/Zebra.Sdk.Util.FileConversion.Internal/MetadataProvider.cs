using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal interface MetadataProvider
	{
		PrinterFileMetadata GetPrinterFileMetadata();
	}
}