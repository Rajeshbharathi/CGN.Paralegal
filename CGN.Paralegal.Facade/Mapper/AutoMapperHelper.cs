#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="AutoMapperHelper.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>LexisNexis</author>
//      <description>
//          This classs represents auto-mapper for facade layer
//      </description>
//      <changelog>
//          <date value="08/06/2014">Created - babugx</date>
//          <date value="08/13/2014">Search Updates Implementation  - Rajesh V</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

using System;
using AutoMapper;

namespace CGN.Paralegal.Facade
{
    /// <summary>
    ///     This classs represents auto-mapper for business layer
    /// </summary>
    internal static class AutoMapperHelper
    {
        /// <summary>
        ///     Initializes the <see cref="AutoMapperHelper" /> class.
        /// </summary>
        static AutoMapperHelper()
        {
            try
            {
                //initialize auto mapper profile here
                Mapper.AddProfile<AutoMapperProfile>();
                Mapper.AssertConfigurationIsValid();
            }
            catch (Exception )
            {
                
                //exception.Trace().Swallow();
            }
        }

    }
    }
