# region File Header

//-----------------------------------------------------------------------------------------
// <copyright file="PredictionScore.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Henry Chen</author>
//      <description>
//          This is a file that contains PredictionScore Entity
//      </description>
//      <changelog>
//          <date value="03/18/2015">Initial version</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

# endregion

namespace CGN.Paralegal.ClientContracts.Analytics
{
    public class PredictionScore
    {
        /// <summary>
        ///     Gets or sets Id
        /// </summary>
        /// <value>
        ///     The Id
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the set.
        /// </summary>
        /// <value>
        /// The name of the set.
        /// </value>
        public string SetName { get; set; }

        /// <summary>
        /// Gets or sets the binder identifier.
        /// </summary>
        /// <value>
        /// The binder identifier.
        /// </value>
        public string BinderId { get; set; }

        /// <summary>
        ///     Gets or sets Recall
        /// </summary>
        /// <value>
        ///     The Recall
        /// </value>
        public double Recall { get; set; }

        /// <summary>
        ///     Gets or sets Precision
        /// </summary>
        /// <value>
        ///     The Precision
        /// </value>
        public double Precision { get; set; }


        /// <summary>
        ///     Gets or sets F1
        /// </summary>
        /// <value>
        ///     The F1
        /// </value>
        public double F1 { get; set; }
    }
}
