using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.Business.DatasetManagement;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DataAccess.MatterManagement;
using LexisNexis.Evolution.DataAccess.ServerManagement;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ConfigurationManagement;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.TransactionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using LexisNexis.Evolution.Worker.Data;
using Moq;
using OverdriveWorkers.Data;

namespace LexisNexis.Evolution.Worker
{
    /// <summary>
    /// Law startup worker to parse law documents, fields and tags to EV documents, fields and tags 
    /// </summary>
    public class LawStartupWorker : WorkerBase, IDisposable
    {
        #region Properties

        private LawImportBEO _jobParams;
        private DatasetBEO _datasetDetails;
        private List<LawFieldBEO> _selectedFields;
        private List<LawTagBEO> _selectedTags;
        private int _batchSize = 100;
        private const int NumericDataType = 108;
        private const int DateDataType = 61;
        private const int TextDataType = 231;

        #endregion

        /// <summary>
        /// To generate message and pass to the next worker with collection of documents in a batch
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            try
            {
                OutputDataPipe.ShouldNotBe(null);
                if (_jobParams == null) _jobParams = GetJobParams(BootParameters);
                var documents =
                    LawBO.GetDocuments(_jobParams.LawCaseId, _selectedFields, _selectedTags, _jobParams.TagFilters);

                if (documents != null)
                {
                    var localDocumentList = new List<RVWDocumentBEO>();
                    foreach (var doc in documents)
                    {
                        doc.DocumentId = GetDocumentId();
                        //Setting the Cross Reference field with LAW DocID by default
                        doc.CrossReferenceFieldValue = doc.LawDocumentId.ToString(CultureInfo.InvariantCulture);
                        localDocumentList.Add(doc);
                        if (localDocumentList.Count%_batchSize != 0) continue;
                        ProcessMessage(localDocumentList);
                        localDocumentList.Clear();
                    }

                    //Sending remaining documents for process
                    if (localDocumentList.Any()) ProcessMessage(localDocumentList);
                }
            }
            catch (Exception ex)
            {
                LogMessage(false, ex.ToUserString());
                ReportToDirector(ex.ToUserString());
                throw;
            }

            LogMessage(true, Constants.ParserSuccessMessage);
            return true;
        }

        /// <summary>
        /// Process the message and sends to the next worker in a batches
        /// </summary>
        /// <param name="documents"></param>
        private void ProcessMessage(List<RVWDocumentBEO> documents)
        {
            // Assign DCN
            if (_jobParams.ImportOptions == ImportOptionsBEO.AppendNew)
            {
                AssignDocumentControlNumber(documents);
            }

            Send(documents);
        }

        /// <summary>
        /// Determines whether the specified value is numeric.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="result">The result.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is numeric; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsNumeric(string value, out long result)
        {
            result = 0;
            try
            {
                result = Int64.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// De Serialize boot parameter
        /// </summary>
        /// <param name="bootParamter"></param>
        /// <returns></returns>
        private static LawImportBEO GetJobParams(string bootParamter)
        {
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(bootParamter);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof (LawImportBEO));

            //Deserialization of bootparameter to get ImportBEO
            return (LawImportBEO) xmlStream.Deserialize(stream);
        }

        /// <summary>
        /// To create dataset tags that are selected for law imprort 
        /// </summary>
        private void CreateSelectedLawTags()
        {
            if (_jobParams == null) _jobParams = GetJobParams(BootParameters);
            if (_jobParams.MappingTags == null) return;
            _selectedTags = _jobParams.MappingTags;
              
           var allDatasetTags=  RVWTagBO.GetTagDefinitions(_jobParams.MatterId.ToString(CultureInfo.InvariantCulture), _jobParams.CollectionId,"All",false, null);

            allDatasetTags = allDatasetTags ?? new List<RVWTagBEO>();

            foreach (var lawTag in _selectedTags.FindAll(x => string.IsNullOrEmpty(x.MappingTagId)))
            {

                var mappedDsTag= allDatasetTags.FirstOrDefault(
                    dsTag => string.IsNullOrEmpty(dsTag.Name) && dsTag.Name.ToLower().Equals(lawTag.MappingTagName));
                if (mappedDsTag != null)
                {
                    //If the tag is already created then continue 
                    lawTag.MappingTagId = mappedDsTag.Id.ToString(CultureInfo.InvariantCulture);
                    continue;
                }
                var tag = new RVWTagBEO
                {
                    Name = lawTag.MappingTagName,
                    IsSystemTag = lawTag.IsSystemTag,
                    IsPrivateTag = false,
                    Scope = TagScope.Document,
                    Type = TagType.Tag,
                    MatterId = _jobParams.MatterId,
                    CollectionId = _jobParams.CollectionId
                };
                lawTag.MappingTagId = RVWTagBO.CreateTag(tag).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// To create dataset fields that are selected for law imprort
        /// </summary>
        private void CreateSelectedLawFields()
        {

            if (_jobParams == null) _jobParams = GetJobParams(BootParameters);
            _selectedFields = _jobParams.MappingFields;
            var dataSetNewFields = new List<FieldBEO>();
            var dataTypes = DataSetTemplateBO.GetDataTypeAndDataFormatList();
            var dataSetExistingFields = DataSetBO.GetDataSetFields(_jobParams.DatasetId, _jobParams.CollectionId);
            dataSetExistingFields = dataSetExistingFields ?? new List<FieldBEO>();
            foreach (var mappedField in _jobParams.MappingFields.FindAll(x => string.IsNullOrEmpty(x.MappingFieldId)))
            {
                if (string.IsNullOrEmpty(mappedField.MappingFieldName)) continue;

                var dataSetField = dataSetExistingFields.FirstOrDefault(
                    dsField =>
                        !string.IsNullOrEmpty(dsField.Name) &&
                        dsField.Name.ToLower().Equals(mappedField.MappingFieldName.ToLower()));
                if (dataSetField != null)
                {
                    //If mapped field already exists in dataset then don't create 
                    continue;
                }

                var field = new FieldBEO
                {
                    Name = mappedField.MappingFieldName,
                    IsReadable = true,
                    IsSingleEntry = true,
                    IsHiddenField = false,
                    IsValidateDateValues = true,
                    CharacterLength = 10,
                    ModifiedBy = _jobParams.CreatedBy
                };
                SetFieldDataType(field, mappedField, dataTypes);
                dataSetNewFields.Add(field);
            }
            if (!dataSetNewFields.Any()) return;
            DataSetBO.AddBulkFields(_jobParams.FolderId, dataSetNewFields, _jobParams.CreatedBy);
            SetFieldIdForCreatedFields(_jobParams.MappingFields);
        }

        /// <summary>
        /// To set field id for created fields
        /// </summary>
        /// <param name="fields"></param>
        private void SetFieldIdForCreatedFields(List<LawFieldBEO> fields)
        {
            _datasetDetails = DataSetBO.GetDataSetDetailForDataSetId(_jobParams.FolderId);
            foreach (var field in fields)
            {
                var datasetField = _datasetDetails.DatasetFieldList.Find(f => f.Name.Equals(field.MappingFieldName));
                if (datasetField != null)
                {
                    field.MappingFieldId = datasetField.ID.ToString(CultureInfo.InvariantCulture);
                }
            }
            _selectedFields = fields;
        }

        /// <summary>
        /// To set field data type
        /// </summary>
        /// <param name="field"></param>
        /// <param name="lawField"></param>
        /// <param name="dataTypes"></param>
        private static void SetFieldDataType(FieldBEO field, LawFieldBEO lawField, IEnumerable<DataTypeBEO> dataTypes)
        {
            switch (lawField.FieldType)
            {
                case LawFieldTypeBEO.Numeric:
                    field.DataType = "NUMERIC";
                    field.FieldType = dataTypes.FirstOrDefault(x => x.DataTypeId == NumericDataType);
                    break;
                case LawFieldTypeBEO.DateTime:
                    field.DataType = "DATE";
                    field.FieldType = dataTypes.FirstOrDefault(x => x.DataTypeId == DateDataType);
                    break;
                default:
                    field.DataType = "TEXT";
                    field.FieldType = dataTypes.FirstOrDefault(x => x.DataTypeId == TextDataType);
                    break;
            }
        }

        /// <summary>
        /// Construct Log Message and Call Log pipe
        /// </summary>
        internal void LogMessage(bool status, string information)
        {
            var log = new List<JobWorkerLog<LawImportLogInfo>>();
            var lawLog = new JobWorkerLog<LawImportLogInfo>
            {
                JobRunId = (!String.IsNullOrEmpty(PipelineId)) ? Convert.ToUInt32(PipelineId) : 0,
                CorrelationId = 0,
                WorkerRoleType = Constants.LawImportStartupWorkerRoleType,
                WorkerInstanceId = WorkerId,
                IsMessage = false,
                Success = status,
                CreatedBy = (!string.IsNullOrEmpty(_jobParams.CreatedBy) ? _jobParams.CreatedBy : "N/A"),
                LogInfo = new LawImportLogInfo {Information = information}
            };
            if (!status)
                lawLog.LogInfo.Message = information;
            log.Add(lawLog);
            SendLog(log);
        }

        /// <summary>
        /// Send Worker response to Pipe.
        /// </summary>
        /// <param name="documents"></param>
        private void Send(List<RVWDocumentBEO> documents)
        {
            OutputDataPipe.ShouldNotBe(null);
            var documentList = new LawDocumentCollection {Documents = documents, Dataset = _datasetDetails};
            var message = new PipeMessageEnvelope
            {
                Body = documentList
            };
            OutputDataPipe.Send(message);
            IncreaseProcessedDocumentsCount(documents.Count);
        }


        /// <summary>
        /// Send Log to Log Worker.
        /// </summary>
        /// <param name="log"></param>
        private void SendLog(List<JobWorkerLog<LawImportLogInfo>> log)
        {
            LogPipe.ShouldNotBe(null);
            var message = new PipeMessageEnvelope
            {
                Body = log
            };
            LogPipe.Send(message);
        }

        /// <summary>
        /// Worker begin work event 
        /// </summary>
        protected override void BeginWork()
        {
            try
            {
                base.BeginWork();
                _jobParams = GetJobParams(BootParameters);
                _jobParams.ShouldNotBe(null);
                _jobParams.FolderId.ShouldBeGreaterThan(0);

                _datasetDetails = DataSetBO.GetDataSetDetailForDataSetId(_jobParams.FolderId);
                var matterDetails =
                    MatterDAO.GetMatterDetails(_jobParams.MatterId.ToString(CultureInfo.InvariantCulture));
                matterDetails.ShouldNotBe(null);
                _datasetDetails.Matter = matterDetails;
                var searchServerDetails = ServerDAO.GetSearchServer(matterDetails.SearchServer.Id);
                searchServerDetails.ShouldNotBe(null);
                _datasetDetails.Matter.SearchServer = searchServerDetails;

                if (!LawBO.TestServerConnection(_jobParams.LawCaseId))
                    ReportToDirector("Failed to connect Law server. Please see application log for details.");

                if (EVHttpContext.CurrentContext == null)
                {
                    // Moq the session
                    MockSession(_jobParams.CreatedBy);
                }

                //Create fields for selected law fields
                CreateSelectedLawFields();

                //Create tags for selected law tags
                CreateSelectedLawTags();

                //Law import batch size for documents
                _batchSize = GetMessageBatchSize();
            }
            catch (Exception ex)
            {
                //Send log infor to Log worker
                LogMessage(false, ex.ToUserString());
                ReportToDirector(ex.ToUserString());
                throw;
            }
        }


        /// <summary>
        /// Mock Session : Windows job doesn't 
        /// </summary>
        private static void MockSession(string createdBy)
        {
            #region Mock

            //MockWebOperationContext webContext = new MockWebOperationContext(createdBy);

            //Mock HttpContext & HttpSession : Calling from Worker so doesn't contain HttpContext. 
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();

            var userProp = UserBO.AuthenticateUsingUserGuid(createdBy);
            userProp.UserGUID = createdBy;
            var userSession = new UserSessionBEO();
            SetUserSession(userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;

            #endregion
        }

        /// <summary>
        /// Sets the usersession object using the UserBusinessEntity details
        /// </summary>
        /// <param name="userProp"></param>
        /// <param name="userSession"></param>
        private static void SetUserSession(UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = userProp.UserGUID;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
        }

        /// <summary>
        /// To assign document control number for new documents
        /// </summary>
        /// <param name="documents"></param>
        private void AssignDocumentControlNumber(List<RVWDocumentBEO> documents)
        {
            long numericPartOfDcn;

            #region Delegate - logic to get complete DCN readily, given the numeric value as input

            Func<string, string> getDcn = delegate(string newLastDcnNumericPart)
            {
                var padString = string.Empty;
                // pad zeros
                if (newLastDcnNumericPart.Length < _datasetDetails.DCNStartWidth.Length)
                {
                    var numberOfZerosTobePadded = _datasetDetails.DCNStartWidth.Length -
                                                  newLastDcnNumericPart.Length;
                    for (var i = 0; i < numberOfZerosTobePadded; i++)
                    {
                        padString += "0";
                    }
                }
                return _datasetDetails.DCNPrefix + padString + newLastDcnNumericPart;
            };

            #endregion Delegate - logic to get complete DCN readily, given the numeric value as input

            using (var lowerTransScope = new EVTransactionScope(TransactionScopeOption.Suppress))
            {
                var currentDcn = DataSetBO.GetLastDocumentControlNumber(_datasetDetails.FolderID);

                // If DCN is not obtained, no documents are imported for the dataset till now.
                // So set current DCN to first DCN value.
                if (string.IsNullOrWhiteSpace(currentDcn))
                {
                    currentDcn = _datasetDetails.DCNPrefix + Constants.StringZero;
                }
                else
                {
                    if (!currentDcn.Contains(_datasetDetails.DCNPrefix))
                    {
                        var currentNumber = Convert.ToInt32(currentDcn);
                        currentNumber = currentNumber - 1;
                        currentDcn = currentNumber.ToString(CultureInfo.InvariantCulture);
                        currentDcn = _datasetDetails.DCNPrefix + currentDcn;
                    }
                }
                // 1) Get Last DCN from EVMaster DB and 2) Pick Numeric part of it
                // throws exception if numeric part couldn't be retrieved, throw Exception.
                if (IsNumeric(currentDcn.Substring(_datasetDetails.DCNPrefix.Length), out numericPartOfDcn))
                {
                    // Update new DCN after bulk add, assuming bulk add would be successful.
                    // The delegate, GetNewLastDCNAfterBulkAdd gets DCN to be updated back to DB.
                    // Delegates takes numeric part of WOULD BE DCN value as input, returns complete DCN - so that it can readily be updated back to Dataset table.
                    var newDcn = getDcn((numericPartOfDcn + documents.Count()).ToString(CultureInfo.InvariantCulture));
                    DataSetBO.UpdateLastDocumentControlNumber(_datasetDetails.FolderID, newDcn);
                    lowerTransScope.Complete();
                }
                else
                {
                    throw new Exception(ErrorCodes.InvalidDCNValueObtainedForDataset);
                }
            }

            #region Assign DCN to all documents

            var dCnIncrementalCounter = numericPartOfDcn;
            foreach (var document in documents)
            {
                dCnIncrementalCounter += 1;
                document.DocumentControlNumber = getDcn(dCnIncrementalCounter.ToString(CultureInfo.InvariantCulture));
            }

            #endregion
        }

        /// <summary>
        /// To get the batch size for law import 
        /// </summary>
        private int GetMessageBatchSize()
        {
            try
            {
                return Convert.ToInt32(ApplicationConfigurationManager.GetValue("LawPipelineBatchSize", "Imports"));
            }
            catch (Exception)
            {
                Tracer.Error("Law Import: Failed to set message batch size for job run id {0}", PipelineId);
                return 0;
            }
        }

        /// <summary>
        /// Get unique document id for each records
        /// First part is unique id for a import job
        /// Scond part is unique for each record
        /// </summary>
        /// <returns></returns>
        private static string GetDocumentId()
        {
            return Guid.NewGuid().ToString().Replace("-", "").ToUpper();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
            }
            // shared cleanup logic
            _disposed = true;
        }
    }
}