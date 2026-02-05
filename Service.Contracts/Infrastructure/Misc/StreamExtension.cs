using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public static class StreamExtension
	{
		public static async Task<byte[]> LoadToMemoryAsync(this Stream content)
		{
			using (var ms = new MemoryStream())
			{
				await content.CopyToAsync(ms, 4096);
				return ms.ToArray();
			}
		}


		public static string ReadAllText(this Stream stream, Encoding encoding)
		{
			using(var ms = new MemoryStream())
			{
				stream.CopyTo(ms, 4096);
				return encoding.GetString(ms.ToArray());
			}
		}


		public static Stream ToMemoryStream(this Stream stream)
		{
			var ms = new MemoryStream();
			using (stream)
			{
				stream.CopyTo(ms, 4096);
			}
			return ms;
		}


		public static bool SupportsLengthProperty(this Stream stream)
		{
			if (stream is DeflateStream)
				return false;
			return true;
		}
	}
}
