using System.Collections.Generic;
using System.Linq;

/*
    Orders Data comparer: Gets differences on provided Orders Data
*/

namespace Service.Contracts
{
	public interface IOrderComparerService
	{
        Dictionary<string, string> GetMatchRow(List<Dictionary<string, string>> savedFile, string key, string value);
        List<string> GetDifferences(Dictionary<string, string> newRow, Dictionary<string, string> savedRow);
        void GetDifferencesByRow(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates);
        void GetDifferencesByColumn(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates, string key, List<string> insertRows);
    }

	class OrderComparerService : IOrderComparerService
    {
        /// <summary>
        /// Gets matching row by a given key
        /// </summary>
        /// <param name="savedFile">Saved Order Data.</param>
        /// <param name="key">key to match any row.</param>
        /// <param name="value">value to match any row.</param>
        public Dictionary<string, string> GetMatchRow(List<Dictionary<string, string>> savedFile, string key, string value)
        {
            var result = new Dictionary<string, string>();
            foreach (var row in savedFile)
            {
                if (row[row.Keys.FirstOrDefault(x => x.Equals(key)).ToString()] == value)
                    return row;
            }

            return result;
        }

        /// <summary>
        /// Gets row differences
        /// </summary>
        /// <param name="newRow">left row to be compared.</param>
        /// <param name="savedRow">right row to be compared.</param>
        public List<string> GetDifferences(Dictionary<string, string> newRow, Dictionary<string, string> savedRow)
        {
            return newRow.Except(savedRow).Select(x => x.Key).ToList();
        }

        /// <summary>
        /// Gets differences on the two provided Orders data
        /// </summary>
        /// <param name="newOrderData">left order data.</param>
        /// <param name="prevOrderData">right order data.</param>
        /// <param name="updates">updated data list by row.</param>
        public void GetDifferencesByRow(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates)
        {
            var differencesRowIds = new List<int>();
            var row = 0;
            var dic = newOrderData[0];

            foreach (var elm in newOrderData)
            {
                var differences = new List<string>();
                var matchingRow = new Dictionary<string, string>();

                matchingRow = prevOrderData.Count - 1 >= row ? prevOrderData[row++] : null;

                if (matchingRow != null && matchingRow.Count > 0)
                {
                    differences = GetDifferences(elm, matchingRow);
                }
                else
                {
                    differences = dic.Keys.ToList();
                }

                updates.Add(differences);
            }
        }

        /// <summary>
        /// Gets differences on the two provided Orders data
        /// </summary>
        /// <param name="newOrderData">left order data.</param>
        /// <param name="prevOrderData">right order data.</param>
        /// <param name="updates">updated data list by row.</param>
        /// <param name="key">key to look at dictionary.</param>
        public void GetDifferencesByColumn(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates, string key, List<string> insertRows)
        {
            var differencesRowIds = new List<int>();
            var extension = newOrderData.FirstOrDefault().ContainsKey(key) ? key : null;
            var dic = newOrderData[0].Keys.ToList();
            var count = 0;
            var dataList = newOrderData.Where(x => x[key] != "").ToList();
            var prevDataList = prevOrderData.Where(x => x[key] != "").ToList();

            foreach (var elm in dataList)
            {
                var differences = new List<string>();
                var matchingRow = new Dictionary<string, string>();

                if (extension != null)
                {
                    var value = elm[elm.Keys.FirstOrDefault(x => x.Equals(extension)).ToString()];
                    matchingRow = GetMatchRow(prevOrderData, extension, value);

                    if (matchingRow.Count == 0 && (dataList.Count != prevDataList.Count))
                    {
                        var newRow = new Dictionary<string, string>();
                        foreach (var e in dic)
                        {
                            newRow.Add(e, string.Empty);
                        }
                        prevOrderData.Insert(count, newRow);
                        insertRows.Add(count.ToString());
                    }
                }

                if (matchingRow != null && matchingRow.Count > 0)
                {
                    differences = GetDifferences(elm, matchingRow);
                }
                else
                {
                    differences = dic;
                }

                updates.Add(differences);
                count++;
            }
        }
    }
}
