using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CGN.Paralegal.UI.Utils
{
    public class ExportHelper
    {

        /// <summary>
        /// Writes to CSV.
        /// </summary>
        /// <param name="sourceTable">The source table.</param>
        /// <param name="includeHeaders">if set to <c>true</c> [include headers].</param>
        /// <returns></returns>
        public static string WriteToCsv(DataTable sourceTable, bool includeHeaders)
        {
            var writer = new StringWriter(CultureInfo.InvariantCulture);
            if (includeHeaders) //Header
            {
                writer.WriteLine(String.Join(",",
                    (from DataColumn column in sourceTable.Columns
                        select ReplaceSpecialCharacters(column.ColumnName))
                        .ToArray()));
            }

            foreach (var items in
                    from DataRow row in sourceTable.Rows
                    select row.ItemArray.Select(o => ReplaceSpecialCharacters(o.ToString())).ToArray())
            {
                writer.WriteLine(String.Join(",", items));
            }

            return writer.ToString();
        }

        /// <summary>
        /// Quotes the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private static string QuoteValue(string value)
        {
            return String.Concat("\"", value.Replace("\"", "\"\""), "\"");
        }

        /// <summary>
        /// Replaces the special characters.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private static string ReplaceSpecialCharacters(string value)
        {
            var sbContent = new StringBuilder(value);
            sbContent.Replace('\n', ' ');
            sbContent.Replace('\r', ' ');
            return QuoteValue(sbContent.ToString());
        }
    }
}