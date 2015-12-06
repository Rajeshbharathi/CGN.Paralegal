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
using CGN.Paralegal.DAL;
using CGN.Paralegal.BusinessEntities.Search;
namespace CGN.Paralegal.Business
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
            Mapper.CreateMap<PLByAOP, PLSearchResult>()
                .ForMember(f => f.PLID, opt => opt.MapFrom(src => src.Para_legal_id))
                .ForMember(f => f.Name, opt => opt.MapFrom(src => src.Para_legal_name))
                .ForMember(f => f.Snippet, opt => opt.MapFrom(src => src.Short_desc));

            Mapper.CreateMap<PLByCity, PLSearchResult>()
                .ForMember(f => f.PLID, opt => opt.MapFrom(src => src.Para_legal_id))
                .ForMember(f => f.Name, opt => opt.MapFrom(src => src.Para_legal_name))
                .ForMember(f => f.Snippet, opt => opt.MapFrom(src => src.Short_desc));

            Mapper.CreateMap<Area_of_Law, AreaOfPractise>()
                .ForMember(f => f.AOPID, opt => opt.MapFrom(src => src.Law_id))
                .ForMember(f => f.AOPName, opt => opt.MapFrom(src => src.Law_name));

            Mapper.CreateMap<DAL.Location, BusinessEntities.Search.Location>()
                .ForMember(f => f.LocationID, opt => opt.MapFrom(src => src.Location_id))
                .ForMember(f => f.LocationName, opt => opt.MapFrom(src => src.Location_name));

        } 
        #endregion

	}
}