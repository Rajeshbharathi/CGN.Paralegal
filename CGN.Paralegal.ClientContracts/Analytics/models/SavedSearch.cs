# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="SavedSearchEntity.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Henry Chen</author>
//      <description>
//          This is a file that contains Saved Search Entity
//      </description>
//      <changelog>
//          <date value="02/24/2015">Initial version</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class SavedSearch
    {
        /// <summary>
        ///     Gets or sets the SavedSearchName
        /// </summary>
        /// <value>
        ///     The SavedSearchName
        /// </value>
        public string SavedSearchName { get; set; }

        /// <summary>
        ///     Gets or sets the SavedSearchId
        /// </summary>
        /// <value>
        ///     The SavedSearchId
        /// </value>
        public string SavedSearchId { get; set; }

        /// <summary>
        ///     Gets or sets the MatterId
        /// </summary>
        /// <value>
        ///     The MatterId
        /// </value>
        public string MatterId { get; set; }

        /// <summary>
        ///     Gets or sets the datasetId
        /// </summary>
        /// <value>
        ///     The datasetId
        /// </value>
        public string DatasetId { get; set; }
    }
}