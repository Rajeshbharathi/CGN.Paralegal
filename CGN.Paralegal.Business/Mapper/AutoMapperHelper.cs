
//-----------------------------------------------------------------------------------------
// <copyright file="AutoMapperHelper.cs" company="Cognizant">
//      Copyright (c) Cognizant. All rights reserved.
// </copyright>
// <header>
//      <author>LexisNexis</author>
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
using CGN.Paralegal.DAL;
using CGN.Paralegal.Infrastructure.ExceptionManagement;

namespace CGN.Paralegal.Business
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


        public static PLSearchResult toSearchResult(this PLByAOP plbyAOP)
        {
            return Mapper.Map<PLByAOP,PLSearchResult>(plbyAOP);
        }

        public static PLSearchResult toSearchResult(this PLByCity plbyCity)
        {
            return Mapper.Map<PLByCity, PLSearchResult>(plbyCity);
        }

        public static AreaOfPractise toAOPEntity(this Area_of_Law AOP)
        {
            return Mapper.Map<Area_of_Law, AreaOfPractise>(AOP);
        }

        public static BusinessEntities.Search.Location toLocation(this DAL.Location location)
        {
            return Mapper.Map<DAL.Location, BusinessEntities.Search.Location>(location);
        }
    }
}
    
