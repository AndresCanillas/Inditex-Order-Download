using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace Service.Contracts
{
	public static class ZipArchiveExtensions
	{
		public static void AddFile(this ZipArchive archive, string entryName, string fileContent)
		{
			var entry = archive.Entries.FirstOrDefault(x => x.Name.Equals(entryName));
			if (entry != null)
				entry.Delete();

			entry = archive.CreateEntry(entryName);
			using (StreamWriter writer = new StreamWriter(entry.Open(), System.Text.Encoding.UTF8))
			{
				writer.Write(fileContent);
			}
		}

		public static string GetFileContent(this ZipArchive archive, string fileName)
		{
			var entry = archive.GetEntry(fileName);
			using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
			{
				var data = reader.ReadToEnd();
				return data;
			}
		}

        public static string GetContent(this ZipArchive arch, string entryName)
        {
            return arch.GetFileContent(entryName);
        }

        public static void AddFromFile(this ZipArchive archive, string filePath, string entryName)
        {
            var entry = archive.CreateEntry(entryName);
            using (var src = File.OpenRead(filePath))
            {
                using (var dst = entry.Open())
                {
                    src.CopyTo(dst);
                }
            }
        }

        public static T GetData<T>(this ZipArchive arch, string entryName)
        {
            var entry = arch.GetEntry(entryName);
            if (entry != null)
            {
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                {
                    var data = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(data);
                }
            }
            else return default(T);
        }
    }
}
