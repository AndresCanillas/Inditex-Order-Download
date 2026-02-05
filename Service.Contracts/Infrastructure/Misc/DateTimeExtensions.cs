using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
    public static class DateTimeExtensions
    {
        public static string ToCSVDateFormat(this DateTime? date)
        {
            return date.HasValue ? ((DateTime)date).ToString("yyyy-MM-dd") : string.Empty;
        }

        public static string ToCSVDateFormat(this DateTime date)
        {
            return ((DateTime)date).ToString("yyyy-MM-dd");
        }
    }
}
