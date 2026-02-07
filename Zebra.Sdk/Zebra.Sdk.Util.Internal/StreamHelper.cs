using System;
using System.IO;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Util.Internal
{
	internal class StreamHelper
	{
		public StreamHelper()
		{
		}

		public static void CopyAndCloseSourceStream(Stream targetStream, Stream sourceStream)
		{
			StreamHelper.CopyAndCloseSourceStream(new HasWrite(targetStream), sourceStream);
		}

		public static void CopyAndCloseSourceStream(Connection targetStream, Stream sourceStream)
		{
			StreamHelper.CopyAndCloseSourceStream(new HasWrite(targetStream), sourceStream);
		}

		private static void CopyAndCloseSourceStream(HasWrite targetStream, Stream sourceStream)
		{
			int num = 0;
			byte[] numArray = new byte[16384];
			for (int i = 0; (long)i < sourceStream.Length; i += num)
			{
				num = sourceStream.Read(numArray, 0, (int)numArray.Length);
				targetStream.Write(numArray, 0, num);
			}
			sourceStream.Dispose();
		}
	}
}