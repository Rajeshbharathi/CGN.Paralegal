# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="DateTimeUtility.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Kutty Murugan Ganesh</author>
//      <description>
//          This is a file that contains DateTimeUtility class 
//      </description>
//      <changelog>
//          <date value="15-04-2011"></date>
//          <date value="08/10/2015">Bugs Fixed #85113</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion



namespace CGN.Paralegal.Infrastructure.Common
{
    public sealed class DateTimeUtility
    {
        private DateTimeUtility()
        {
        }
        /// <summary>
        /// Helper method to determine the valid date format for the given field date format allowed in .net runtime
        /// </summary>
        /// <param name="fieldDateFormat"></param>
        /// <returns></returns>
        public static string GetLegitimateDateFormat(string fieldDateFormat)
        {
            string appDateFormat = string.Empty;

            if (!string.IsNullOrEmpty(fieldDateFormat))
            {
                switch (fieldDateFormat)
                {
                    case Constants.FIELD_TYPE_DDMMYYY:
                        appDateFormat = Constants.FIELD_TYPE_DDMMYYY_APP_FORMAT;
                        break;
                    case Constants.FIELD_TYPE_MMDDYYY:
                        appDateFormat = Constants.FIELD_TYPE_MMDDYYY_APP_FORMAT;
                        break;
                    case Constants.FIELD_TYPE_DATEFORMAT_DISPLAY:
                        appDateFormat = Constants.FIELD_TYPE_YYYYMMDD_APP_FORMAT;
                        break;
                    case Constants.FIELD_TYPE_DDMMYYY_HHMMSS:
                        appDateFormat = Constants.FIELD_TYPE_DDMMYYY_HHMMSS_APP_FORMAT;
                        break;
                    case Constants.FIELD_TYPE_MMDDYYY_HHMMSS:
                        appDateFormat = Constants.FIELD_TYPE_MMDDYYY_HHMMSS_APP_FORMAT;
                        break;
                    case Constants.FIELD_TYPE_DATEFORMAT_HHMMSS_DISPLAY:
                        appDateFormat = Constants.FIELD_TYPE_YYYYMMDD_HHMMSS_APP_FORMAT;
                        break;
                    default:
                        appDateFormat = fieldDateFormat;
                        break;
                }
            }
            return appDateFormat;
        }
    }
}
