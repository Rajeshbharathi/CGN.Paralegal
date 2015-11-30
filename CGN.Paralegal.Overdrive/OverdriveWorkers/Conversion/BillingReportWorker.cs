#region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="BillingReportWorker.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>LexisNexis</author>
//      <description>
//          This file has Billing Report Worker class which generates the CSV output
//      </description>
//      <changelog>
//          <date value="11/06/2014">Task # 178804 -Billing Report enhancement</date>
//      </changelog>
// </header>
//----------------------------------------------------------------------------------------- 
#endregion
using System.IO;
using LexisNexis.Evolution.Business.Reports;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.EVContainer;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;

namespace OverdriveWorkers.Conversion
{
    class BillingReportWorker :WorkerBase
    {
        private const string ReportFactoryUnityContainerName = "ReportFactory";
        private BillingReportParams _billingReportParams;
        
        /// <summary>
        /// Property to resolve the Report factory from unity container
        /// </summary>
        private IReportFactory ReportFactory
        {
            get { return EVUnityContainer.Resolve<IReportFactory>(ReportFactoryUnityContainerName); }
        }

        /// <summary>
        /// Overload method from the base class to start the worker process
        /// </summary>
        protected override void BeginWork()
        {
            base.BeginWork();


            _billingReportParams =
                (BillingReportParams) XmlUtility.DeserializeObject(BootParameters, typeof (BillingReportParams));
                
            //Pre conditions
            _billingReportParams.ShouldNotBe(null);
            _billingReportParams.FolderList.ShouldNotBe(null);
           
        }

        /// <summary>
        /// Overload to execute the worker process
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            ReportFactory.GenerateBillingReport(_billingReportParams);
            
            return File.Exists(_billingReportParams.TargetFolder); 
        }
        
    }
}
