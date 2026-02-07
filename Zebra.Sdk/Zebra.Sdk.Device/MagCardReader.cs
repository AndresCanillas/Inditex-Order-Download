using System;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       Provides access to the magnetic card reader, for devices equipped with one.
	///       </summary>
	public interface MagCardReader
	{
		/// <summary>
		///       Activates the device's magnetic card reader, if present, and waits for a card to be swiped.
		///       </summary>
		/// <param name="timeoutMS">The amount of time in milliseconds to enable the reader and wait for a card to be swiped.</param>
		/// <returns>An array of three strings corresponding to the tracks of the card. If a track could not be read that
		///       string will be empty.</returns>
		string[] Read(int timeoutMS);
	}
}