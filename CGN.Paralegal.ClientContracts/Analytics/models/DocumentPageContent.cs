# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="DocumentPageContent.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Henry Chen</author>
//      <description>
//          This is a file that contains DocumentPageContent for holding document paging information
//      </description>
//      <changelog>
//          <date value="02/09/2015">initial version</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class DocumentPageContent
    {
        /// <summary>
        /// Gets or sets the index of the page.
        /// </summary>
        /// <value>
        /// The index of the page.
        /// </value>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the total page count.
        /// </summary>
        /// <value>
        /// The total page count.
        /// </value>
        public int TotalPageCount { get; set; }
    }
}