namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       A status connection to a Link-OS printer. The status connection requires Link-OS firmware 2.5 or higher. This 
	///       connection will not block the printing channel, nor can it print.
	///       </summary>
	public interface StatusConnection : Connection
	{

	}
}