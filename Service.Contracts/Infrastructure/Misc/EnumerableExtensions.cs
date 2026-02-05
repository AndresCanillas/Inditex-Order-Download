using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Service.Contracts
{
    public static class EnumerableExtensions
    {
        public static DataTable ToDataTable<T>(this IEnumerable<T> entityList) where T : class
        {
            
            var table = new DataTable();
            

            if (typeof(T) != typeof(ExpandoObject))
            {
                var properties = typeof(T).GetProperties();
                foreach (var property in properties)
                {
                    var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    table.Columns.Add(property.Name, type);
                }
                foreach (var entity in entityList)
                {
                    table.Rows.Add(properties.Select(p => p.GetValue(entity, null)).ToArray());
                }
                return table;
            }else
            {
                return DynamicToDataTable((entityList as IEnumerable<ExpandoObject>));
            }
        }

        // https://gist.github.com/XelaNimed/d66bb06873f62fc66a693955bb0cd17d
        private static DataTable DynamicToDataTable(IEnumerable<ExpandoObject> list, string tableName = "dynamic_table")
        {

            if (list == null || list.Count() == 0)
            {
                return null;
            }

            //build columns
            var props = (IDictionary<string, object>)list.ElementAt(0);
            var t = new DataTable(tableName);
            foreach (var prop in props)
            {
                t.Columns.Add(new DataColumn(prop.Key, typeof(string)));
            }
            //add rows
            foreach (var row in list)
            {
                var data = t.NewRow();
                foreach (var prop in (IDictionary<string, object>)row)
                {
                    data[prop.Key] = prop.Value;
                }
                t.Rows.Add(data);
            }
            return t;
        }
    }
}
