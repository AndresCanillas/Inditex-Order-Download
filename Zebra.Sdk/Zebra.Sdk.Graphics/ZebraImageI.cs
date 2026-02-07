using System;

namespace Zebra.Sdk.Graphics
{
	/// <summary>
	///       Contains methods used to query attributes of an image formatted for a Zebra printer.
	///       </summary>
	public interface ZebraImageI : IDisposable
	{
		/// <summary>
		///       Gets the image's height in pixels.
		///       </summary>
		int Height
		{
			get;
		}

		/// <summary>
		///       Gets the image's width in pixels.
		///       </summary>
		int Width
		{
			get;
		}
	}
}