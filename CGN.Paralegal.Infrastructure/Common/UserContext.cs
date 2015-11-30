#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="UserContext.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>LexisNexis</author>
//      <description>
//          This file has UserContext class
//      </description>
//      <change log>
//          <date value="2/20/2014">Created File</date>
//      </change log>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

using System;

namespace CGN.Paralegal.Infrastructure.Common
{
    /// <summary>
    ///     UserContext holds the user context details
    /// </summary>
    [Serializable]
    internal class UserContext : IUserContext
    {
        private readonly string _guid;
        private readonly string _id;
        private readonly string _name;


        /// <summary>
        ///     Initializes a new instance of the <see cref="UserContext" /> class.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="firstName">The name.</param>
        /// <param name="lastName"></param>
        /// <param name="domain"></param>
        public UserContext(string guid, string id, string firstName, string lastName, string domain)
        {
            _name = string.Format("{0} {1}", firstName, lastName);
            _id = id;
            if (!string.IsNullOrEmpty(domain) && !domain.Equals("N/A", StringComparison.InvariantCultureIgnoreCase))
                _id = string.Format("{0}\\{1}", domain, id);
            //Ad domain user's first name and last name information is not available in EV so putting user id as a user name  
            //as UserName database column in Vault.AUD_Log (also EVMaster.AUD_Log) is a not null 
            if (String.IsNullOrWhiteSpace(_name))
                _name = _id;
            _guid = guid;
        }

        /// <summary>
        ///     Gets or sets the unique identifier.
        /// </summary>
        /// <value>
        ///     The unique identifier.
        /// </value>
        public string Guid
        {
            get { return _guid; }
        }

        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        public string Id
        {
            get { return _id; }
        }


        /// <summary>
        ///     Gets the name.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        public string Name
        {
            get { return _name; }
        }
    }
}