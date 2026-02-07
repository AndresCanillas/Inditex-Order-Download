using System;
using System.IO;

namespace Zebra.Sdk.Comm.Internal
{
	public interface ZebraSocket
	{
		void Close();

		void Connect();

		BinaryReader GetInputStream();

		BinaryWriter GetOutputStream();
	}
}