//---------------------------------------------------------------------------------------------------
// <copyright file="EVHttpContext.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ravi/Visu</author>
//      <description>
//          This class provides the HttpContext for EVConcordance app
//      </description>
//      <changelog>
//          <date value=""></date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

namespace CGN.Paralegal.Infrastructure
{
    using System;
    using System.Web;

    /// <summary>
    /// This class provides the HttpContext for EVConcordance app
    /// </summary>
    public static class EVHttpContext
    {
        #region Member variable

        // When EVHttpContext is used as part of the actual ASP.NET code HttpContext.Current is not null and
        // getter always returns new HttpContextWrapper(HttpContext.Current);

        // When EVHttpContext is used as part of the unit test HttpContext.Current is null and
        // CurrentContext works as a normal static property with the backing static field context.

        // When EVHttpContext is used as part of the job HttpContext.Current is null and
        // CurrentContext works as a normal static property, BUT multiple jobs can be running in the same process
        // on the different threads, so to prevent them from stepping on each other "context" must be [ThreadStatic].

        [ThreadStatic]
        private static HttpContextBase context; 

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the CurrentContext Property
        /// </summary>
        public static HttpContextBase CurrentContext
        {
            get
            {
                /*if HttpContext.Current is  null then use the _context (mocked for unit testing purposes)*/
                if (HttpContext.Current != null)
                {
                    context = new HttpContextWrapper(HttpContext.Current);
                }

                return context;
            }

            set
            {
                context = value;
            }
        } 

        #endregion
    }
}
