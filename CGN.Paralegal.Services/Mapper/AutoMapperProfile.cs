#region Header

//-----------------------------------------------------------------------------------------
// <copyright file="AutoMapperProfile.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>LexisNexis</author>
//      <description>
//          This classs represents auto-mapper profile for business layer
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
using CGN.Paralegal.BusinessEntities.Search;
using CGN.Paralegal.SearchContracts.Search;
namespace CGN.Paralegal.Services
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

            DoSearchMapping();
		}


        #region Search 
        private static void DoSearchMapping()
        {
            Mapper.CreateMap<CGN.Paralegal.BusinessEntities.Search.PLSearchResult, CGN.Paralegal.SearchContracts.Search.PLSearchResult>();


            Mapper.CreateMap<CGN.Paralegal.BusinessEntities.Search.PLSearchResult, CGN.Paralegal.SearchContracts.Search.PLSearchResult>();

            Mapper.CreateMap<BusinessEntities.Search.AreaOfPractise, ServiceContracts.Search.AreaOfPractise>();

            Mapper.CreateMap<BusinessEntities.Search.Location, ServiceContracts.Search.Location>();
        } 
        #endregion

	}
}