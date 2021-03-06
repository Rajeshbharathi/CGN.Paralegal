﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34003
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LexisNexis.Evolution.Overdrive.DirectorCoreServices {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="DirectorCoreServices.IDirectorCoreServices")]
    public interface IDirectorCoreServices {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDirectorCoreServices/GetOpenJobs", ReplyAction="http://tempuri.org/IDirectorCoreServices/GetOpenJobsResponse")]
        LexisNexis.Evolution.Overdrive.OpenJobs GetOpenJobs(string machineName);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDirectorCoreServices/GetJobInfo", ReplyAction="http://tempuri.org/IDirectorCoreServices/GetJobInfoResponse")]
        LexisNexis.Evolution.Overdrive.JobInfo GetJobInfo(string pipelineId, string machineName);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IDirectorCoreServices/GetWorkerStatistics", ReplyAction="http://tempuri.org/IDirectorCoreServices/GetWorkerStatisticsResponse")]
        LexisNexis.Evolution.Overdrive.WorkerStatistics[] GetWorkerStatistics(string pipelineId, string machineName);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IDirectorCoreServicesChannel : LexisNexis.Evolution.Overdrive.DirectorCoreServices.IDirectorCoreServices, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class DirectorCoreServicesClient : System.ServiceModel.ClientBase<LexisNexis.Evolution.Overdrive.DirectorCoreServices.IDirectorCoreServices>, LexisNexis.Evolution.Overdrive.DirectorCoreServices.IDirectorCoreServices {
        
        public DirectorCoreServicesClient() {
        }
        
        public DirectorCoreServicesClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public DirectorCoreServicesClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public DirectorCoreServicesClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public DirectorCoreServicesClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public LexisNexis.Evolution.Overdrive.OpenJobs GetOpenJobs(string machineName) {
            return base.Channel.GetOpenJobs(machineName);
        }
        
        public LexisNexis.Evolution.Overdrive.JobInfo GetJobInfo(string pipelineId, string machineName) {
            return base.Channel.GetJobInfo(pipelineId, machineName);
        }
        
        public LexisNexis.Evolution.Overdrive.WorkerStatistics[] GetWorkerStatistics(string pipelineId, string machineName) {
            return base.Channel.GetWorkerStatistics(pipelineId, machineName);
        }
    }
}
