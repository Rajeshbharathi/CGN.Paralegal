using System;
using System.Collections.Generic;
using LexisNexis.Evolution.Overdrive;

namespace LexisNexis.Evolution.Worker
{
    using System.Runtime.CompilerServices;

    public class ClassicJobWorker : WorkerBase
    {
        private static readonly Dictionary<string, string[]> ClassicJobHandlers = new Dictionary<string,string[]>() 
        { 
            // 1. Pipeline moniker - the name of that pipeline in Director's configuration
            // 2. Name of the class which executes this type of job with the namespace
            // 3. Name of the assembly (without .dll) where that class is located
            {"DeleteDataSet",               new[] {"DataSetDelete.DataSetDeleteJob", "ClassicJobs"}},
            {"SearchAlerts",                new[] {"Alerts.AlertsJob", "ClassicJobs"}},
            {"GlobalReplace",               new[] {"GlobalReplace.FindandReplaceJob", "ClassicJobs"}},
            {"Deduplication",               new[] {"Deduplication.DeduplicationJob", "ClassicJobs"}},
            {"UpdateServerStatus",          new[] {"ServerManagement.UpdateServerStatusJob", "ClassicJobs"}},
            {"EmailDocuments",              new[] {"Email.EmailJob", "ClassicJobs"}},
            {"PrintDocuments",              new[] {"Print.PrintJob", "ClassicJobs"}},
            {"DownloadDocuments",           new[] {"ReviewerBulkTag.ReviewerBulkTagJob", "ClassicJobs"}},
            {"FindAndReplaceRedactionXml",  new[] {"FindReplaceRedactionXML.FindReplaceRedactionXML", "ClassicJobs"}},
            {"RefreshReports",              new[] {"RefreshReports.RefreshReportsJob", "ClassicJobs"}},
            {"ReviewerBulkTag",             new[] {"ReviewerBulkTag.ReviewerBulkTagJob", "ClassicJobs"}},
            {"UpdateReviewSet",             new[] {"UpdateReviewSet.UpdateReviewSetJob", "ClassicJobs"}},
            {"MergeReviewSet",              new[] {"MergeReviewSet.MergeReviewSetJob", "ClassicJobs"}},
            {"SplitReviewSet",              new[] {"SplitReviewSet.SplitReviewSetJob", "ClassicJobs"}},
            {"PrivilegeLog",                new[] {"PrivilegeLog.PrivilegeLogJob", "ClassicJobs"}},
            {"DCBOpticonExports",           new[] {"DcbOpticonExports.DCBOpticonExportJob", "OverdriveWorkers32"}},
            {"SaveSearchResults",           new[] {"SaveSearchResults.SaveSearchResultsJob", "ClassicJobs"}},
            {"CompareSaveSearchResults",    new[] {"CompareSavedSearchResultsJob.CompareSavedSearchResultsJob", "ClassicJobs"}},
            {"DeleteDocumentField",         new[] {"ReviewerBulkTag.ReviewerBulkTagJob", "ClassicJobs"}},
            {"BulkPrint",                   new[] {"BulkPrint.BulkPrintJob", "ClassicJobs"}},
            {"ConvertDCBLinksToCaseMap",    new[] {"ConvertDCBLinksToCaseMap.ConvertDCBLinksToCaseMap", "ClassicJobs"}},
            {"DeleteTag",                   new[] {"BulkTagDelete.BulkTagDelete", "ClassicJobs"}},
            {"SendDocumentLinksToCaseMap",  new[] {"SendDocumentLinksToCaseMap.SendDocumentLinksToCaseMap", "ClassicJobs"}},
            {"BulkDocumentDelete",          new[] {"BulkDocumentDelete.BulkDocumentDelete", "ClassicJobs"}},
            {"FullDocumentStaticClustering", new[] {"ReviewerBulkTag.ReviewerBulkTagJob", "ClassicJobs"}},
        };
 
        private dynamic jobHandler;

        protected override void BeginWork()
        {
            base.BeginWork();

            var classicJobHandler = ClassicJobHandlers[PipelineType.Moniker];
            var classicJobHandlerType = "LexisNexis.Evolution.BatchJobs." + classicJobHandler[0];
            this.jobHandler = Activator.CreateInstance(classicJobHandler[1], classicJobHandlerType).Unwrap();
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        protected override bool GenerateMessage()
        {
            if (null != this.jobHandler)
            {
                this.jobHandler.DoWork
                    (
                        WorkAssignment.JobId,
                        Convert.ToInt32(PipelineId),
                        BootParameters,
                        WorkAssignment.ScheduleRunDuration,
                        WorkAssignment.ScheduleCreatedBy,
                        WorkAssignment.NotificationId,
                        WorkAssignment.Frequency
                    );
            }
            return true;
        }
    }

    public class DeleteAuditLogClassicWorker                : ClassicJobWorker { }
    public class DeleteDataSetClassicWorker                 : ClassicJobWorker { }
    public class SearchAlertsClassicWorker                  : ClassicJobWorker { }
    public class GlobalReplaceClassicWorker                 : ClassicJobWorker { }
    public class DeduplicationClassicWorker                 : ClassicJobWorker { }
    public class UpdateServerStatusClassicWorker            : ClassicJobWorker { }
    public class EmailDocumentsClassicWorker                : ClassicJobWorker { }
    public class PrintDocumentsClassicWorker                : ClassicJobWorker { }
    public class DownloadDocumentsClassicWorker             : ClassicJobWorker { }
    public class FindAndReplaceRedactionXmlClassicWorker    : ClassicJobWorker { }
    public class RefreshReportsClassicWorker                : ClassicJobWorker { }
    public class ReviewerBulkTagClassicWorker               : ClassicJobWorker { }
    public class UpdateReviewSetClassicWorker               : ClassicJobWorker { }
    public class MergeReviewSetClassicWorker                : ClassicJobWorker { }
    public class SplitReviewSetClassicWorker                : ClassicJobWorker { }
    public class PrivilegeLogClassicWorker                  : ClassicJobWorker { }
    public class DCBOpticonExportsClassicWorker             : ClassicJobWorker { }
    public class SaveSearchResultsClassicWorker             : ClassicJobWorker { }
    public class CompareSaveSearchResultsClassicWorker      : ClassicJobWorker { }
    public class DeleteDocumentFieldClassicWorker           : ClassicJobWorker { }
    public class BulkPrintClassicWorker                     : ClassicJobWorker { }
    public class ConvertDCBLinksToCaseMapClassicWorker      : ClassicJobWorker { }
    public class DeleteTagClassicWorker                     : ClassicJobWorker { }
    public class SendDocumentLinksToCaseMapClassicWorker    : ClassicJobWorker { }
    public class BulkDocumentDeleteClassicWorker            : ClassicJobWorker { }
    public class FullDocumentStaticClusteringClassicWorker  : ClassicJobWorker { }

}
