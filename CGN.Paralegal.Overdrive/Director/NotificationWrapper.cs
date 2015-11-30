#region File Header
//---------------------------------------------------------------------------------------------------
// <copyright file="NotificationWrapper.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Kostya/Nagaraju</author>
//      <description>
//          This file contains the NotificationWrapper class
//      </description>
//      <changelog>
//          <date value="09/30/2013">Task # 152663 -ADM -ADMIN - 006 -  Reprocess Select All Implementation Part 2
//          <date value="11/06/2014">Task # 178804 -Billing Report enhancement</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion


using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.Documents;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Business.JobManagement;
using LexisNexis.Evolution.Business.NotificationManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using System.Web;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.ReviewSet;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.Reports;
using LexisNexis.Evolution.Overdrive.App_Resources;

namespace LexisNexis.Evolution.Overdrive
{
    internal class NotificationWrapper : IDisposable
    {
        private const int BillingReportJobTypeId = 42;
        
        private const string DownloadHandlerURL = "..\\DownloadHandlers\\DownloadHandler.ashx?url={0}";
        public NotificationWrapper()
        {
            DebugMode = false;
        }

        public bool DebugMode { get; set; }

        #region Constants

        private const string NotificationMessageCustomLoadFileAppendSuccessMessage =
            "New documents have been added to the Dataset ";

        private const string NotificationMessageCustomLoadFileAppendMessagePart2 = " as part of the Import Job : ";

        private const string NotificationMessageCustomLoadFileAppendFailureMessage =
            "New documents could not be successfully imported to the Dataset ";

        private const string NotificationMessageCustomLoadFileOverlaySuccessMessagePart1 =
            "Documents belonging to the Dataset ";

        private const string NotificationMessageCustomLoadFileOverlaySuccessMessagePart2 =
            "have been modified as part of the Import Overlay job:";

        private const string NotificationMessageCustomLoadFileOverlayFailureMessagePart1 =
            "Documents could not be successfully modified for Dataset ";

        private const string NotificationMessageCustomLoadFileOverlayFailureMessagePart2 =
            " as part of the Import Overlay Job: ";

        private const string NotificationMessageCustomLoadFileOverlayFailureMessagePart3 =
            ". Please refer to the Import log for details.";

        private const string NotificationMessageCustomConversionResultsExportError =
            "Problem in exporting conversion results ";
        private const string ConversionResultsExportTitleOpenDiv = @"<div><div>Title:Conversion Results Export </div>";
        private const string ConversionResultsExportFilePathOpenDiv = @"<div> File Location:";
        private const string ConversionResultsExportFilePathCloseDiv = @"</div>";
        private const string ConversionResultsDownloadLinkOpenAnchor = @"<a target='_blank' href=' ";
        private const string ConversionResultsDownloadLinkText = @"'>Conversion Results Ready To Download ";
        private const string ConversionResultsDownLoadLinkCloseAnchor = "</a></div>";

        private const string ConversionResultsImageTag =
            @"<img alt='Conversion Results Export' src='../Images/IconDOCtext16.png'/>";

        private const string ConversionResultsExportDownloadHandler =
            "../ManageConversions/ConversionResultsExport.ashx?JobId=";

        private const string AnalysissetDocumentsResultsDownloadLinkText = @"'>Analysisset Documents Results Ready To Download ";
        private const string AnalysissetDocumentsResultsImageTag =
            @"<img alt='Analysisset Documents Results Export' src='../Images/IconDOCtext16.png'/>";

        #endregion

        #region "Reviewer Bulk Tag Job"

        internal const string Table = "<table>";
        internal const string Row = "<tr>";
        internal const string Column = "<td>";
        internal const string CloseColumn = "</td>";
        internal const string CloseRow = "</tr>";
        internal const string CloseTable = "</table>";
        internal const string Header = "<th align=\"left\">";
        internal const string CloseHeader = "</th>";
        internal const string HtmlBold = "<b>";
        internal const string HtmlCloseBold = "</b>";
        internal const string SpaceHiphenSpace = " - ";

        internal const string NotificationMessageStartTime = "Task Start Time: ";
        internal const string NotificationMessageEndTime = "Task End Time: ";
        internal const string NotificationMessageLocation = "Location: ";
        internal const string NotificationMessageTagName = "Tag Name: ";
        internal const string Instance = " Instance: ";

        internal const string NotificationMessageHeadingForTagging = "Bulk Tagging Completed";
        internal const string NotificationMessageForTagging = "Your tagging operation has completed.";
        internal const string NotificationMessageForTaggingDocumentsTagged = "Documents Tagged: ";
        internal const string NotificationMessageForTaggingDocumentsAlreadyTagged = "Documents Already Tagged: ";
        internal const string NotificationMessageForTaggingDocumentsFailed = "Documents Failed To Tag: ";
        internal const string NotificationMessageHeadingForUntagging = "Bulk Untagging Completed";
        internal const string NotificationMessageHeadingForUntaggingSuspend = "Bulk Untagging Suspended";
        internal const string NotificationMessageForUntagging = "Your untagging operation has completed.";
        internal const string NotificationMessageForUntaggingSuspend = "Your untagging operation has been suspended";
        internal const string NotificationMessageForUntaggingDocumentsUnTagged = "Documents Untagged: ";
        internal const string NotificationMessageForUntaggingDocumentsNotUntagged = "Documents Not Untagged: ";

        #endregion

        internal void SendNotifications(ActiveJob job, Director.JobStatus jobStatus, string notificationMessage)
        {
            if (DebugMode) return;
            try
            {
                if (null != job)
                {
                    JobBusinessEntity jobDetails = null;
                    var jobBusinessEntity = JobMgmtBO.GetJobDetails(job.JobId.ToString());
                    var userName = jobBusinessEntity.CreatedBy;
                    jobDetails = job.BusinessEntity;
                    var userBusinessEntity = UserBO.GetUser(userName);

                    #region Construct the notification message object

                    NotificationMessageBEO notificationMessageBeo = null;
                    try
                    {

                        notificationMessageBeo = new NotificationMessageBEO
                                                     {
                                                         NotificationId = job.Beo.JobNotificationId,
                                                         CreatedByUserGuid = userBusinessEntity.UserGUID,
                                                         CreatedByUserName =
                                                             (userBusinessEntity.DomainName.Equals(@"N/A"))
                                                                 ? userBusinessEntity.UserId
                                                                 : userBusinessEntity.DomainName + @"\" +
                                                                   userBusinessEntity.UserId,
                                                         SubscriptionTypeName = jobDetails.TypeName,
                                                         FolderId = jobDetails.FolderID
                                                     };
                    }
                    catch
                    {
                        Tracer.Error("Unable to construct notification message for Job Id: {0}", job.JobId);
                    }

                    #endregion

                    #region Call Notification API

                    if (null != jobDetails && null != notificationMessageBeo &&
                        ((0 != notificationMessageBeo.NotificationId ||
                          !string.IsNullOrEmpty(notificationMessageBeo.SubscriptionTypeName)) &&
                         !string.IsNullOrEmpty(notificationMessageBeo.CreatedByUserGuid)))
                    {
                        #region Construct notification message

                        var message = new StringBuilder();
                        message.Append(string.IsNullOrEmpty(jobDetails.TypeName) ? string.Empty : " Type: ");
                        message.Append(jobDetails.Visibility
                                           ? jobDetails.TypeName
                                           : jobDetails.TypeName.Replace("Job", "Task"));
                        message.Append("<br/>");
                        message.Append(jobDetails.Name);
                        if (job.Beo != null)
                        {
                            message.Append(" Instance: ");
                            message.Append(job.Beo.JobRunId);
                        }
                        message.Append("<br/>");
                        message.Append("Folder: ");
                        message.Append(jobDetails.FolderName);
                        message.Append(" ");
                        message.Append("<br/>");
                        message.Append("Status: ");
                        message.Append(notificationMessage);

                        #region Job type specific custom message

                        try
                        {
                            switch (job.JobTypeId)
                            {
                                case 14: // Load File Job custom message as per FSD.
                                    {
                                        var stream = new StringReader(job.Beo.BootParameters);
                                        var xmlStream = new XmlSerializer(typeof(ImportBEO));
                                        var bootParameters = xmlStream.Deserialize(stream) as ImportBEO;
                                        stream.Close();

                                        if (null != bootParameters &&
                                            string.IsNullOrEmpty(bootParameters.NotificationMessage))
                                        {
                                            if (bootParameters.IsAppend)
                                            {
                                                if (jobStatus == Director.JobStatus.Completed)
                                                {
                                                    message.Append(NotificationMessageCustomLoadFileAppendSuccessMessage);
                                                    message.Append(jobDetails.FolderName);
                                                    message.Append(NotificationMessageCustomLoadFileAppendMessagePart2);
                                                    message.Append(job.JobId.ToString());
                                                }
                                                else
                                                {
                                                    message.Append(NotificationMessageCustomLoadFileAppendFailureMessage);
                                                    message.Append(jobDetails.FolderName);
                                                    message.Append(NotificationMessageCustomLoadFileAppendMessagePart2);
                                                    message.Append(job.JobId.ToString());
                                                }
                                            }
                                            else
                                            {
                                                if (jobStatus == Director.JobStatus.Completed)
                                                {
                                                    message.Append(
                                                        NotificationMessageCustomLoadFileOverlaySuccessMessagePart1);
                                                    message.Append(jobDetails.FolderName);
                                                    message.Append(
                                                        NotificationMessageCustomLoadFileOverlaySuccessMessagePart2);
                                                    message.Append(job.JobId.ToString());
                                                }
                                                else
                                                {
                                                    message.Append(
                                                        NotificationMessageCustomLoadFileOverlayFailureMessagePart1);
                                                    message.Append(jobDetails.FolderName);
                                                    message.Append(
                                                        NotificationMessageCustomLoadFileOverlayFailureMessagePart2);
                                                    message.Append(job.JobId.ToString());
                                                    message.Append(
                                                        NotificationMessageCustomLoadFileOverlayFailureMessagePart3);
                                                }
                                            }
                                        }
                                    }
                                    notificationMessageBeo.SendDefaultMessage = (jobStatus ==
                                                                                 Director.JobStatus.Completed);
                                    break;
                                case 16: // Reviewer bulk tag job custom message as per FSD.
                                    ConstructBulkTagNotification(job, jobStatus, notificationMessageBeo, jobBusinessEntity,notificationMessage);
                                    break;
                                case 38:
                                    message.Append(jobStatus == Director.JobStatus.Completed
                                                       ? ConstructNotificationMessageForConversionResultsExport(job)
                                                       : NotificationMessageCustomConversionResultsExportError);
                                    break;
                                case 54:
                                    if (jobStatus == Director.JobStatus.Completed)
                                    {
                                        message.Append(ConstructNotificationMessageForExportAnalysisSetDocuments(job));
                                    }
                                    break;
                                case BillingReportJobTypeId:
                                    
                                    message.Append(ConstructBillingReportNotificationMessage(job,jobStatus));
                                    break;
                            }
                        }
                        catch
                        {
                            Tracer.Error("Unable to set custom notification message for Job Id:{0}", job.JobId);
                        }

                        #endregion

                        if (job.JobTypeId != 16)
                        {
                            notificationMessageBeo.NotificationSubject = message.ToString().Replace("<br/>", "  ");
                            notificationMessageBeo.NotificationBody = message.ToString();
                        }


                        #endregion

                        #region Send notification message by invoking notifications API

                        try
                        {
                            if (!string.IsNullOrEmpty(notificationMessageBeo.NotificationBody))
                                NotificationBO.SendNotificationMessage(notificationMessageBeo);
                        }
                        catch
                        {
                            Tracer.Error("Unable to send notification message for Job Id: {0}", job.JobId);
                        }

                        #endregion
                        if (job.JobTypeId == 14 || job.JobTypeId == 16)  // Load File Import
                            return; //For Load File Import- Custom message will be sent by default notification message.  
                        if (jobStatus == Director.JobStatus.Completed && job.Beo.JobNotificationId > 0)
                        {
                            var customNotify = new NotificationMessageBEO
                                {
                                    NotificationId = job.Beo.JobNotificationId,
                                    SendDefaultMessage = true,
                                    CreatedByUserGuid = userBusinessEntity.UserGUID
                                };

                            NotificationBO.SendNotificationMessage(customNotify);
                        }
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                ex.AddUsrMsg("Unable to send notification message.").Trace().Swallow();
            }
        }

        private string ConstructBillingReportNotificationMessage(ActiveJob job,Director.JobStatus jobStatus)
        {
            var billingReportParams = (BillingReportParams)
                                       XmlUtility.DeserializeObject(job.BootParameters.ToString(),
                                       typeof(BillingReportParams));
            
            var downloadHandler = String.Format(DownloadHandlerURL,
               HttpUtility.UrlEncode(billingReportParams.TargetFolder));
            var notificationSubMessage = String.Format(Notifications.BillingReportGenerated,
                downloadHandler);

            return (jobStatus == Director.JobStatus.Completed )?
               notificationSubMessage : String.Empty;
        }
        /// <summary>
        /// This method is responsible to construct the Notification subject and Notification Body for the message to be delivered
        /// </summary>
        /// <param name="job"></param>
        /// <param name="jobStatus"></param>
        /// <param name="notificationMessageBeo"></param>
        /// <param name="message"></param>
        private static void ConstructBulkTagNotification(ActiveJob job, Director.JobStatus jobStatus,
            NotificationMessageBEO notificationMessageBeo, JobBusinessEntity jobDetails, string notificationMessage)
        {
            var stream = new StringReader(job.Beo.BootParameters);
            var xmlStream = new XmlSerializer(typeof(BulkTagJobBusinessEntity));
            var bootParameters = xmlStream.Deserialize(stream) as BulkTagJobBusinessEntity;
            stream.Close();

            var message = new StringBuilder();
            message.Append(string.IsNullOrEmpty(jobDetails.TypeName) ? string.Empty : " Type: ");
            message.Append(jobDetails.Visibility
                               ? jobDetails.TypeName
                               : jobDetails.TypeName.Replace("Job", "Task"));
            message.Append("<br/>");
            message.Append(jobDetails.Name);
            if (job.Beo != null)
            {
                message.Append(" Instance: ");
                message.Append(job.Beo.JobRunId);
            }
            message.Append("<br/>");
            message.Append("Folder: ");
            message.Append(jobDetails.FolderName);
            message.Append(" ");
            message.Append("<br/>");
            message.Append("Status: ");
            message.Append(notificationMessage);

            if (null != bootParameters)
            {
                var tagLog = RVWTagBO.GetTagLog(Convert.ToInt32(bootParameters.TagDetails.DatasetId), job.JobId);               

                if (jobStatus == Director.JobStatus.Completed)
                {
                    notificationMessageBeo.NotificationSubject = message.ToString().Replace("<br/>", "  ");
                    if (tagLog != null)
                    {
                        notificationMessageBeo.NotificationBody = ConstructNotificationBodyForReviewerBulkTag(job, bootParameters, tagLog, jobDetails);
                    }
                }
                else
                {
                    notificationMessageBeo.NotificationBody = notificationMessageBeo.NotificationSubject = message.ToString().Replace("<br/>", "  ");
                }
            }
            notificationMessageBeo.SendDefaultMessage = (jobStatus ==
                                                         Director.JobStatus.Completed);
        }

      private static string ConstructNotificationBodyForReviewerBulkTag
          (ActiveJob activeJob, BulkTagJobBusinessEntity bulkTagJobBeo, TagLogBEO tagLogBeo, JobBusinessEntity jobBeo)
        {
            //Add notification message to be sent
            var notificationMessage = new StringBuilder();
            notificationMessage.Append(Table);

            if (bulkTagJobBeo.IsOperationTagging)
            {
                notificationMessage.Append(Row);
                notificationMessage.Append(Header);
                notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageHeadingForTagging));
                notificationMessage.Append(CloseHeader);
                notificationMessage.Append(CloseRow);
                notificationMessage.Append(Row);
                notificationMessage.Append(Column);
                notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageForTagging));
                notificationMessage.Append(CloseColumn);
                notificationMessage.Append(CloseRow);

                GenerateGeneralNotificationDetailsForReviewerBulkTagJob(activeJob, bulkTagJobBeo, notificationMessage, jobBeo);

                notificationMessage.Append(Row);
                notificationMessage.Append(Column);
                notificationMessage.Append(HtmlBold);
                notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageForTaggingDocumentsTagged));
                notificationMessage.Append(HtmlCloseBold);
                notificationMessage.Append(
                    HttpUtility.HtmlEncode(tagLogBeo.DocumentTag.ToString(CultureInfo.InvariantCulture)));
                notificationMessage.Append(CloseColumn);
                notificationMessage.Append(CloseRow);
                notificationMessage.Append(Row);
                notificationMessage.Append(Column);
                notificationMessage.Append(HtmlBold);
                notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageForTaggingDocumentsAlreadyTagged));
                notificationMessage.Append(HtmlCloseBold);
                notificationMessage.Append(
                    HttpUtility.HtmlEncode(tagLogBeo.AlreadyTag.ToString(CultureInfo.InvariantCulture)));
                notificationMessage.Append(CloseColumn);
                notificationMessage.Append(CloseRow);
                notificationMessage.Append(Row);
                notificationMessage.Append(Column);
                notificationMessage.Append(HtmlBold);
                notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageForTaggingDocumentsFailed));
                notificationMessage.Append(HtmlCloseBold);
                notificationMessage.Append(
                    HttpUtility.HtmlEncode(tagLogBeo.FailedTag.ToString(CultureInfo.InvariantCulture)));
                notificationMessage.Append(CloseColumn);
                notificationMessage.Append(CloseRow);
            }
            else
            {
                notificationMessage.Append(Row);
                notificationMessage.Append(Header);
                notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageHeadingForUntagging));
                notificationMessage.Append(CloseHeader);
                notificationMessage.Append(CloseRow);
                notificationMessage.Append(Row);
                notificationMessage.Append(Column);
                notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageForUntagging));
                notificationMessage.Append(CloseColumn);
                notificationMessage.Append(CloseRow);

                GenerateGeneralNotificationDetailsForReviewerBulkTagJob(activeJob, bulkTagJobBeo, notificationMessage, jobBeo);

                notificationMessage.Append(Row);
                notificationMessage.Append(Column);
                notificationMessage.Append(HtmlBold);
                notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageForUntaggingDocumentsUnTagged));
                notificationMessage.Append(HtmlCloseBold);
                notificationMessage.Append(
                    HttpUtility.HtmlEncode(tagLogBeo.DocumentTag.ToString(CultureInfo.InvariantCulture)));
                notificationMessage.Append(CloseColumn);
                notificationMessage.Append(CloseRow);
                notificationMessage.Append(Row);
                notificationMessage.Append(Column);
                notificationMessage.Append(HtmlBold);
                notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageForUntaggingDocumentsNotUntagged));
                notificationMessage.Append(HtmlCloseBold);
                notificationMessage.Append(
                    HttpUtility.HtmlEncode(
                        (tagLogBeo.FailedTag + tagLogBeo.AlreadyTag).ToString(CultureInfo.InvariantCulture)));
                notificationMessage.Append(CloseColumn);
                notificationMessage.Append(CloseRow);
            }

            notificationMessage.Append(CloseTable);
            return notificationMessage.ToString();
        }

      /// <summary>
      /// Generate part of notification message common to all events during bulk tagging
      /// </summary>
      /// <param name="bulkTagRecord"></param>
      /// <param name="notificationMessage"></param>
      private static void GenerateGeneralNotificationDetailsForReviewerBulkTagJob
          (ActiveJob job, BulkTagJobBusinessEntity bulkTagJobBeo, StringBuilder notificationMessage, JobBusinessEntity jobBeo)
      {
          notificationMessage.Append(Row);
          notificationMessage.Append(Column);
          notificationMessage.Append(HtmlBold);
          notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageStartTime));
          notificationMessage.Append(HtmlCloseBold);

          notificationMessage.Append(HttpUtility.HtmlEncode(jobBeo.JobScheduleStartDate.ConvertToUserTime()));

          notificationMessage.Append(CloseColumn);
          notificationMessage.Append(CloseRow);
          notificationMessage.Append(Row);
          notificationMessage.Append(Column);
          notificationMessage.Append(HtmlBold);
          notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageEndTime));
          notificationMessage.Append(HtmlCloseBold);

          notificationMessage.Append(HttpUtility.HtmlEncode(jobBeo.JobCompletedDate.ConvertToUserTime()));

          notificationMessage.Append(CloseColumn);
          notificationMessage.Append(CloseRow);
          notificationMessage.Append(Row);
          notificationMessage.Append(Column);
          notificationMessage.Append(HtmlBold);
          notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageLocation));
          notificationMessage.Append(HtmlCloseBold);


          notificationMessage.Append(HttpUtility.HtmlEncode(GetTaggingLocation(bulkTagJobBeo)));


          notificationMessage.Append(CloseColumn);
          notificationMessage.Append(CloseRow);

          notificationMessage.Append(Row);
          notificationMessage.Append(Column);
          notificationMessage.Append(HtmlBold);
          notificationMessage.Append(HttpUtility.HtmlEncode(NotificationMessageTagName));
          notificationMessage.Append(HtmlCloseBold);

          notificationMessage.Append(HttpUtility.HtmlEncode(bulkTagJobBeo.TagDetails.Name));

          notificationMessage.Append(CloseColumn);
          notificationMessage.Append(CloseRow);
      }

      /// <summary>
      /// Get the tag location
      /// </summary>
      /// <param name="bulkTagRecord"></param>
      /// <returns></returns>
      private static string GetTaggingLocation(BulkTagJobBusinessEntity bulkTagJobBeo)
      {
          var location = string.Empty;

          if (!string.IsNullOrEmpty(bulkTagJobBeo.DocumentListDetails.SearchContext.ReviewSetId))
          {
              var reviewSetDetails = ReviewSetBO.GetReviewSetDetails
                  (bulkTagJobBeo.DocumentListDetails.SearchContext.MatterId.ToString(CultureInfo.InvariantCulture), bulkTagJobBeo.DocumentListDetails.SearchContext.ReviewSetId);
              if (reviewSetDetails != null)
              {
                  location = reviewSetDetails.ReviewSetName;
              }
          }
          else
          {
              var dataSetDetails = DataSetBO.GetDataSetDetailForDataSetId(bulkTagJobBeo.DocumentListDetails.SearchContext.DataSetId);
              if (dataSetDetails != null)
              {
                  location = dataSetDetails.FolderName;
              }
          }
          return location;
      }

        /// <summary>
        /// Constructs the notification message for conversion results export.
        /// </summary>
        /// <param name="activeJob">The active job.</param>
        /// <returns>notification message for conversion results export job</returns>
        private static string ConstructNotificationMessageForConversionResultsExport(ActiveJob activeJob)
        {
            var stream = new StringReader(activeJob.Beo.BootParameters);
            var xmlStream = new XmlSerializer(typeof(ConversionResultsExportJobParam));
            var conversionResultsExportJobParameters = xmlStream.Deserialize(stream) as ConversionResultsExportJobParam;

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(ConversionResultsExportTitleOpenDiv);
            if (conversionResultsExportJobParameters != null)
            {
                stringBuilder.Append(ConversionResultsExportFilePathOpenDiv);
                stringBuilder.Append(conversionResultsExportJobParameters.TargetFileName);
                stringBuilder.Append(ConversionResultsExportFilePathCloseDiv);
            }
            stringBuilder.Append(ConversionResultsImageTag);
            stringBuilder.Append(ConversionResultsDownloadLinkOpenAnchor);
            stringBuilder.Append(ConversionResultsExportDownloadHandler +
                                 activeJob.JobId.ToString(CultureInfo.InvariantCulture));
            stringBuilder.Append(ConversionResultsDownloadLinkText);
            stringBuilder.Append(ConversionResultsDownLoadLinkCloseAnchor);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Construct Notification message for Export AnalysisSet Documents
        /// </summary>
        /// <param name="activeJob"></param>
        /// <returns></returns>
        private static string ConstructNotificationMessageForExportAnalysisSetDocuments(ActiveJob activeJob)
        {
            try
            {
                var stringBuilder = new StringBuilder();
                var documentQuery = (DocumentQuery)XmlUtility.DeserializeObject(activeJob.Beo.BootParameters, typeof(DocumentQuery));
                var dataset= DataSetBO.GetDataSetDetailForDataSetId(documentQuery.DatasetId);
                var jobInfo = JobMgmtBO.GetJobDetails(activeJob.JobId.ToString(CultureInfo.InvariantCulture));
                var filePath = string.Format("{0}\\{1}{2}", dataset.CompressedFileExtractionLocation, jobInfo.Name, ".csv");
                if (!string.IsNullOrEmpty(filePath))
                {
                    stringBuilder.Append(ConversionResultsExportFilePathOpenDiv);
                    stringBuilder.Append(filePath);
                    stringBuilder.Append(ConversionResultsExportFilePathCloseDiv);
                }
                stringBuilder.Append(AnalysissetDocumentsResultsImageTag);
                stringBuilder.Append(ConversionResultsDownloadLinkOpenAnchor);
                stringBuilder.Append(ConversionResultsExportDownloadHandler +
                                     activeJob.JobId.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(AnalysissetDocumentsResultsDownloadLinkText);
                stringBuilder.Append(ConversionResultsDownLoadLinkCloseAnchor);
                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
            return string.Empty;
        }


        public void Dispose()
        {
        }
    }
}