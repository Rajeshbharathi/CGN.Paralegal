using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LexisNexis.Evolution.Worker
{
    public class RecordTokenizer
    {
        public RecordTokenizer(char fieldDelimeter, char quoteCharcater)
        {
            QuoteDelimeter = quoteCharcater;
            FieldDelimeter = fieldDelimeter;
        }


        /// <summary>
        /// Field(or Column) delimiter
        /// </summary>
        private char FieldDelimeter { get; set; }

        /// <summary>
        /// Quote delimiter
        /// </summary>
        private char QuoteDelimeter { get; set; }


        /// <summary>
        /// Get Content Field value from record and remove content text in record
        /// </summary>
        /// <param name="record">record text</param>
        /// <param name="fieldNumber">Field index</param>
        /// <returns>Field value</returns>
        public string GetContentFieldValueAndRemoveContentInRecord(ref string record, int fieldNumber)
        {
            var fieldData = ParseRecordAsFields(record);
            if (fieldData.Count() > fieldNumber)
            {
                string content = fieldData[fieldNumber];
                fieldData[fieldNumber] = string.Empty; //Remove content text 
                record = string.Join(FieldDelimeter.ToString(CultureInfo.InvariantCulture), fieldData); //reconstruct record after remove content text
                var contentValue = CleanFieldsData(new[] { content });
                return contentValue[0];
            }
            return string.Empty;
        }


        /// <summary>
        /// Parse record based on delimiters
        /// </summary>
        /// <param name="record">record text</param>
        /// <returns>List of fields</returns>
        public string[] ParseRecord(string record)
        {
            var fieldData = ParseRecordAsFields(record);
            return CleanFieldsData(fieldData);
        }

        /// <summary>
        /// Parse Record as Field(Column)
        /// </summary>
        /// <param name="record">record text</param>
        /// <returns>List of fields</returns>
        private string[] ParseRecordAsFields(string record)
        {
            var pattern = string.Format(Constants.StringPattern, Regex.Escape(FieldDelimeter.ToString(CultureInfo.InvariantCulture)), Regex.Escape(QuoteDelimeter.ToString(CultureInfo.InvariantCulture)));
            //Split record using pattern
            return Regex.Split(record, pattern);
        }

        /// <summary>
        /// Clean Fields Data(Remove quote character(
        /// </summary>
        /// <param name="fieldData">List of fields</param>
        /// <returns>List of fields</returns>
        private string[] CleanFieldsData(IEnumerable<string> fieldData)
        {
            //Get Fields with removing quote charcater 
            return fieldData.Select(x => x.TrimStart(QuoteDelimeter).TrimEnd(Constants.RecordSplitter.ToCharArray()).TrimEnd(QuoteDelimeter).ToString(CultureInfo.InvariantCulture)).ToArray();

        }


    }
}
