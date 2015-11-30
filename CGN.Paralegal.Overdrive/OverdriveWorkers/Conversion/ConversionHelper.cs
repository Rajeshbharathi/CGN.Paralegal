#region Header
//-----------------------------------------------------------------------------------------
// <copyright file="ConversionHelper.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Kostya/Nagaraju</author>
//      <description>
//          This file contains all the  methods related to  ConversionHelper class
//      </description>
//      <changelog>
//          <date value="10/23/2013">Bug  # 154585 -ADM -ADMIN - 006 - Fix to avoid forever  conversion validation and blocking behavior for document conversion time out
//          <date value="10/23/2013">Bug  # 156607 - Fix to avoid forever  conversion validation for documents without hearbeat files by having absolute timeout 
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
#endregion

using System;
using LexisNexis.Evolution.Business.CentralizedConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.ServerManagement;

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// ConversionHelper
    /// </summary>
    internal static class ConversionHelper
    {
        /// <summary>
        /// The document conversion timeout configuration name
        /// </summary>
        private const string DocumentConversionTimeoutConfigName = "Document Conversion Timeout";
        /// <summary>
        /// The document global conversion timeout configuration name
        /// </summary>
        private const string DocumentGlobalConversionTimeoutConfigName = "Document Conversion Validation Timeout";

        /// <summary>
        /// Gets the document conversion time out.
        /// </summary>
        /// <returns>document conversion timeout</returns>
        internal static TimeSpan GetDocumentConversionTimeout()
        {
            var documentConversionTimeoutInMinutes = 2;
            try
            {
                var hostId = ServerConnectivity.GetHostIPAddress();
                var conversionTimeOutConfigValue = CmgServiceConfigBO.GetServiceConfigurationsforConfig(hostId,
                                                                                                        "System Configuration Service",
                                                                                                        DocumentConversionTimeoutConfigName);
                documentConversionTimeoutInMinutes = Convert.ToInt32(conversionTimeOutConfigValue);

            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Unable to read the document conversion timeout from  server configuration ");
                ex.Trace().Swallow();
            }

            return new TimeSpan(0, documentConversionTimeoutInMinutes, 0);
        }
        /// <summary>
        /// Gets the document global conversion timeout.
        /// </summary>
        /// <returns>document conversion time out</returns>
        internal static TimeSpan GetDocumentGlobalConversionTimeout()
        {

            var documentGlobalConversionTimeoutInMinutes = 1440;//24 hours
            try
            {
                var hostId = ServerConnectivity.GetHostIPAddress();
                var conversionTimeOutConfigValue = CmgServiceConfigBO.GetServiceConfigurationsforConfig(hostId,
                                                                                                        "System Configuration Service",
                                                                                                        DocumentGlobalConversionTimeoutConfigName);
                documentGlobalConversionTimeoutInMinutes = Convert.ToInt32(conversionTimeOutConfigValue);

            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Unable to read the document global conversion timeout from  server configuration ");
                ex.Trace().Swallow();
            }

            return new TimeSpan(0, documentGlobalConversionTimeoutInMinutes, 0);
        }
        
    }
}
