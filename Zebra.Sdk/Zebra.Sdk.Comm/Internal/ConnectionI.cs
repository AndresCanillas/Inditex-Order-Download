using System;

namespace Zebra.Sdk.Comm.Internal
{
	internal interface ConnectionI
	{
		string Manufacturer
		{
			get;
		}

		int MaxDataToWrite
		{
			get;
			set;
		}

		byte[] Read(int maxBytesToRead, bool exitOnFirstRead);

		byte[] Read(int maxBytesToRead);

		void SetReadTimeout(int readTimeout);
	}
}