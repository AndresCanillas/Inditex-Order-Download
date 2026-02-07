using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Text;
using Microsoft.Win32;

namespace WebLink.Contracts.Models
{
    public class FontRepository : IFontRepository
    {
        private IFactory factory;
        private ITempFileService temp;
        private string path = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

        public FontRepository(
            IFactory factory,
            ITempFileService temp
            )
        {
			this.factory = factory;
            this.temp = temp;
        }

        public IEnumerable<string> GetList()
        {
            var extensions = new List<string>() { "ttf", "woff", "otf", "fnt", "ttc" };
            var fonts = new List<string>();
            var fileEntries = Directory.GetFiles(path).Where(x => extensions.Any(x.ToLower().Split('.').Contains)).ToList();

            foreach (var font in fileEntries)
            {
                fonts.Add(font.Split("\\").LastOrDefault());
            }

            return fonts;
        }

        public Dictionary<string, string> GetUpdatedDate()
        {
            var fonts = new Dictionary<string, string>();
            var fontList = GetList();

            foreach (var font in fontList)
            {
                var filePath = Path.Combine(path, font.Split("\\").LastOrDefault());

                fonts.Add(font.Split("\\").LastOrDefault(), File.GetLastWriteTime(filePath).Ticks.ToString());
            }

            return fonts;
        }


        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        public string UploadFont(string fileName, Stream content)
        {
            var filePath = Path.Combine(path, fileName);
			var userData = factory.GetInstance<IUserData>();

            using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                content.CopyTo(fs, 4096);
                fs.Close();
            }

            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", fileName.Split('.')[0], fileName, RegistryValueKind.String);

            File.SetLastWriteTime(filePath, DateTime.Now);

            return fileName;
        }


        public Stream GetFont(string fileName)
        {
            var filePath = Path.Combine(path, fileName);

            if (File.Exists(filePath))
                return FileStoreHelper.GetFileStream(filePath);

            return null;
        }

        public void DeleteFont(string fileName)
        {
            var filePath = Path.Combine(path, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
