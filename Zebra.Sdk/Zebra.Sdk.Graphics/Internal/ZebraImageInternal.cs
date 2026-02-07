using System;
using System.IO;
using Zebra.Sdk.Graphics;

namespace Zebra.Sdk.Graphics.Internal
{
	internal interface ZebraImageInternal : ZebraImageI, IDisposable
	{
		byte[] GetDitheredB64EncodedPng();

		int[] GetRow(int row);

		bool ScaleImage(int width, int height);

		void WriteDitheredPng(Stream destinationStream);
	}
}