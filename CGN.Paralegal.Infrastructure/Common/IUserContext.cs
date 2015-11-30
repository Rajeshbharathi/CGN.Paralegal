


#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="IUserContext.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>LexisNexis</author>
//      <description>
//          This file has IUserContext interface.
//      </description>
//      <changelog>
//          <date value="2/20/2014">Created File</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

namespace CGN.Paralegal.Infrastructure.Common
{
    /// <summary>
    ///     IUserContext class is the contract for user context class
    /// </summary>
    public interface IUserContext
    {
        /// <summary>
        ///     Gets or sets the unique identifier.
        /// </summary>
        /// <value>
        ///     The unique identifier.
        /// </value>
        string Guid { get; }

        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        string Id { get; }


        /// <summary>
        ///     Gets the name.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        string Name { get; }
    }
}
