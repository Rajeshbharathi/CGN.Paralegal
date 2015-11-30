#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="AutoMapperProfile.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>LexisNexis</author>
//      <description>
//          This classs represents auto-mapper profile for facade layer
//      </description>
//      <changelog>
//          <date value="08/06/2014">Created - babugx</date>
//          <date value="08/13/2014">Search Updates Implementation  - Rajesh V</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------

#endregion

using AutoMapper;


namespace CGN.Paralegal.Facade
{
    /// <summary>
    ///     This class implements the AutoMapperProfile for Business layer
    /// </summary>
    internal class AutoMapperProfile : Profile
    {
        protected override void Configure()
        {
            /*Auto mapper mappings should be bi-directional.
             (i.e) there should a mapping to convert data contract to business entity and 
            there should be separate mapping to convert business entitty to data contact */

        }

    }
}
