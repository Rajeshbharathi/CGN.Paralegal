# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="TagEntity.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Henry Chen</author>
//      <description>
//          This is a file that contains Tag Entity
//      </description>
//      <changelog>
//          <date value="02/24/2015">Initial version</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class Tag
    {

        /// <summary>
        ///     Gets or sets the Name
        /// </summary>
        /// <value>
        ///     The Name
        /// </value>
        public string m_Name { get; set; }

        /// <summary>
        ///     Gets or sets the TagDisplayName
        /// </summary>
        /// <value>
        ///     The TagDisplayName
        /// </value>
        public string m_TagDisplayName { get; set; }

        /// <summary>
        ///     Gets or sets the Id
        /// </summary>
        /// <value>
        ///     The Id
        /// </value>
        public string m_Id { get; set; }

        /// <summary>
        ///     Gets or sets the MatterId
        /// </summary>
        /// <value>
        ///     The MatterId
        /// </value>
        public string MatterId { get; set; }

        /// <summary>
        ///     Gets or sets the DatasetId
        /// </summary>
        /// <value>
        ///     The DatasetId
        /// </value>
        public string DatasetId { get; set; }
    }
}