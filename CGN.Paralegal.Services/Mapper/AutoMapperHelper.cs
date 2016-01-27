
//-----------------------------------------------------------------------------------------
// <copyright file="AutoMapperHelper.cs" company="Cognizant">
//      Copyright (c) Cognizant. All rights reserved.
// </copyright>
// <header>
//      <author>Cognizant</author>
//      <description>
//          This classs represents auto-mapper helper for business layer
//      </description>
//      <changelog>
//          <date value="12/04/2015">Search Updates Implementation  - Rajesh V</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------


using System;
using AutoMapper;
using System.Collections.Generic;
using CGN.Paralegal.BusinessEntities.Search;
using CGN.Paralegal.ServiceContracts.Search;
using CGN.Paralegal.Infrastructure.ExceptionManagement;

namespace CGN.Paralegal.Services
{
    /// <summary>
    ///     This class represents auto-mapper for business layer
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
                Mapper.AddProfile(new AutoMapperProfile());
                Mapper.AssertConfigurationIsValid();
            }
            catch (Exception exception)
            {
                exception.Trace().Swallow();
            }
        }

        public static CGN.Paralegal.SearchContracts.Search.PLSearchResult toSearchResult(this CGN.Paralegal.BusinessEntities.Search.PLSearchResult plbyAOP)
        {
            return Mapper.Map<CGN.Paralegal.BusinessEntities.Search.PLSearchResult, CGN.Paralegal.SearchContracts.Search.PLSearchResult>(plbyAOP);
        }

        public static  ServiceContracts.Search.AreaOfPractise toAOP(this BusinessEntities.Search.AreaOfPractise AOP)
        {
            return Mapper.Map<BusinessEntities.Search.AreaOfPractise, ServiceContracts.Search.AreaOfPractise>(AOP);
        }

        public static ServiceContracts.Search.Location toLocation(this BusinessEntities.Search.Location location)
        {
            return Mapper.Map<BusinessEntities.Search.Location, ServiceContracts.Search.Location>(location);
        }

        public static ServiceContracts.Search.PLDetail toPLDetail(this BusinessEntities.Search.PLDetail plDetail)
        {
            return Mapper.Map<BusinessEntities.Search.PLDetail, ServiceContracts.Search.PLDetail>(plDetail);
        }
    }
}
    
