# region File Header
/// <copyright file="Constants.cs" company="LexisNexis">
///     Copyright (c) LexisNexis. All rights reserved.
/// </copyright>
/// <header>
///      <author>Swamy</author>
///     <description>
///         This file contain Constatns
///      </description>
///      <changelog>
///          <date value="10/1/2011"></date>
///      </changelog>
/// </header>
# endregion

namespace LexisNexis.Evolution.BatchJobs.ServerManagement
{
    class Constants
    {
        private Constants()
        {
        }
        public const string JobName = "Update Server Status";
        public const string MethodInitialize = "In Initialize Method";
        public const string GenerateTasksMethod = "In Generate Tasks Method";
        public const string DoAtomicWorkMethod = "In Do Atomic Work Method";
        public const string JobStartMessage = "Started at : ";
        public const string JobEndMessage = "Completed at : ";

        public const string JobFailMessage = "Failed at : ";
        //public const string AuditJob = "AUDIT Job";
        internal const string JobTypeName = "Update Server Status";

        internal const string SpEvSvrUpdateServerStatus = "EV_SVR_Update_ServerStatus";
        internal const string ParamInServerID = "@in_sServerid";
        internal const string ParamInServerType = "@in_iServerType";
        internal const string ParamInServerStatus = "@in_bAvailabilitystatus";
        internal const string ParaOutStatus = "@out_iStatus";
        internal const string LineBreak = "<br/>";

    }
}
