using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Service.Contracts
{
    public static class Rfc4180Writer
    {
        public static void CreateStream(DataTable sourceTable, StreamWriter writer, bool includeHeaders)
        {

            CultureInfo culture = CultureInfo.InstalledUICulture; 

            // Obtener el separador de listas
            string listSeparator = culture.TextInfo.ListSeparator;

            if(includeHeaders)
            {
                IEnumerable<String> headerValues = sourceTable.Columns
                    .OfType<DataColumn>()
                    .Select(column => QuoteValue(column.ColumnName));
                writer.WriteLine(String.Join(listSeparator, headerValues));
            }

            IEnumerable<String> items = null;

            foreach(DataRow row in sourceTable.Rows)
            {
                items = row.ItemArray.Select(o => QuoteValue(o.ToString()));
                writer.WriteLine(String.Join(listSeparator, items));
            }

            writer.Flush();
        }

        // TODO: use regional separator https://www.codeproject.com/Articles/80083/Detecting-User-Regional-Settings-In-The-Web-Browse
        public static string QuoteValue(string str)
        {
            CultureInfo culture = CultureInfo.InstalledUICulture; //CultureInfo.CurrentUICulture;

            // Obtener el separador de listas
            string listSeparator = culture.TextInfo.ListSeparator;

            return QuoteValue(str, listSeparator);
        }

        public static string QuoteValue(string str, char listSeparator)
        {
            return QuoteValue(str, listSeparator.ToString());
        }

        public static string QuoteValue(string str, string listSeparator)
        {
            str = string.IsNullOrEmpty(str) ? string.Empty : str;
            listSeparator = string.IsNullOrEmpty(listSeparator) ? string.Empty : listSeparator;

            bool mustQuote = (str.Contains(listSeparator) || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
            if (mustQuote)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\"");
                foreach (char nextChar in str)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                        sb.Append("\"");
                }
                sb.Append("\"");
                return sb.ToString();
            }

            return str;
        }
    }
}
