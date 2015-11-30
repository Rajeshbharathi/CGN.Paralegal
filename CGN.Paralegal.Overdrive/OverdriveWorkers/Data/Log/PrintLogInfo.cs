//---------------------------------------------------------------------------------------------------
// <copyright file="PrintLogInfo.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Madhavan Murrali</author>
//      <description>
//          This file contains the PrintLogInfo.
//      </description>
//      <changelog>
//          <date value="22/4/2013">ADM – PRINTING – 001 Implementation</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------
using System;
using System.Text;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    class PrintLogInfo : BaseWorkerProcessLogInfo
    {
        public string DCN { get; set; }
        public string DatasetName { get; set; }
        public string PrintJobName { get; set; }


        public static implicit operator string(PrintLogInfo log)
        {
            var info = new StringBuilder();
            info.Append("DCN: " + log.DCN + " \n, ");
            info.Append("Print Job Name: " + log.PrintJobName + " \n, ");
            info.Append("Dataset Name: " + log.DatasetName + " \n, ");
            return info.ToString();
        }

    }
}
