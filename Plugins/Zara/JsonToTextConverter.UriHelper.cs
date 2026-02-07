using Services.Core;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inidtex.ZaraExterlLables
{
    public static partial class JsonToTextConverter
    {
        private static class UriHelper
        {
            public static bool IsUrl(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return false;

                return Uri.TryCreate(value, UriKind.Absolute, out _);
            }

            public static string ExtractFileNameWithoutExtension(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return string.Empty;

                var path = value;
                if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                    path = uri.AbsolutePath;
                else
                    path = value.Split('?')[0].Split('#')[0];

                var fileName = System.IO.Path.GetFileName(path);
                return System.IO.Path.GetFileNameWithoutExtension(fileName);
            }
        }

        
    }
}
