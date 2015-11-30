# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="EvCorlibManager.cs">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish</author>
//      <description>
//          This is a file that contains EvCorlibManager class 
//      </description>
//      <changelog>
//          <date value="19-August-2010"></date>
//          <date value="5/7/2014">NLog Exception bug fixing for Null Reference exception </date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using evcorlib;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Manages EVCorlib specific, file extraction related functionality
    /// </summary>
    public sealed class EvCorlibManager : IDisposable
    {

        public enum EvCorlibManagerType
        {
            Normal,
            Oulook,
            Lotus
        }

        #region Constants and error codes section
        static class ErrorCodes
        {
            /// <summary>
            /// Error while creating an object of EvCorlib Manager.
            /// </summary>
            public const string EvCorlibManagerObjectCreateError = "EvCorlibManagerObjectCreateError";

            /// <summary>
            /// Errors in constructor of EVCorlibManager
            /// </summary>
            public const string EVCorlibManagerInstantiateError = "EVCorlibManagerInstantiateError";

            /// <summary>
            /// EVCorlib Session Initialize Error (occurs in the EVCorlib COM library)
            /// </summary>
            public const string EDLoaderSessionInitializeError = "EDLoaderSessionInitializeError";

            /// <summary>
            /// Error representing unsupported mail store type. Supported mail stores are in Enum LexisNexis.Evolution.DocumentExtractionUtilities.EMailType
            /// </summary>
            public const string UnsupportedMailStore = "UnsupportedMailStore";

            /// <summary>
            /// Error representing failure to process mail store
            /// </summary>
            public const string ProcessMailStoreError = "ProcessMailStoreError";

            /// <summary>
            /// Error representing 0 mail stores to process
            /// </summary>
            public const string NoMailStoresError = "NoMailStoresError";

            /// <summary>
            /// Error representing failure to list mail store. mail store processing can't proceed further.
            /// </summary>
            public const string EDLoaderListMailStoreError = "EDLoaderListMailStoreFailure";

            /// <summary>
            /// Error representing EVCorlibMailstore conversion to EmailTypeConversion
            /// </summary>
            public const string MailStoreConversionError = "MailStoreConverstionError";

            /// <summary>
            /// Represents error when mail processor initialize fails
            /// </summary>
            public const string MailProcessorInitializeError = "MailProcessorInitializeFailure";

            /// <summary>
            /// Represents error extracting email messages (not while processing entry ids)
            /// </summary>
            public const string EmailMessageExtractionError = "EmailMessageExtractionError";

            /// <summary>
            /// Represents error creating password file
            /// </summary>
            public const string PasswordFileCreateError = "EDLoaderPasswordFileCreateError";

            /// <summary>
            /// Failed to dispose EvCorlib manager 
            /// </summary>
            public const string CleanupFailure = "EvCorlibCleanupFailure";

            /// <summary>
            /// EvCorlib failure to process the file.
            /// </summary>
            public const string ProcessFileError = "ProcessFileError";

            /// <summary>
            /// EvCorlib failure to create an output file
            /// </summary>
            public const string NoOutpuFile = "NoOutpuFile";

            /// <summary>
            /// Represents error when no entry ids are provided input to MailProcessor
            /// </summary>
            public const string EntryIDsUnavailable = "EntryIDsUnavailable";

            /// <summary>
            /// Error depicting no files to extract in the input.
            /// </summary>
            public const string NoFilesToProcess = "NoFilesToProcess";

        }

        static class Constants
        {
            /// <summary>
            /// Represents MS Outlook e-mail store
            /// </summary>
            public const string OutlookPrefix = @"pst://";

            /// <summary>
            /// Represents Notes e-mail store
            /// </summary>
            public const string LotusPrefix = @"nsf://";

            /// <summary>
            /// Represents folder name for storing extracted archives.
            /// </summary>
            public const string ExtractionLocationDirectoryName = "ExtractedArchives - Exception Class: {0}\nDescription: {3}\nAssociatedInputUri: {1}\nAssociatedUuid: {2}";

            /// <summary>
            /// Constant error description format string
            /// </summary>
            public const string EvCorlibExceptionDescription = "Exception Class: {0}\nDescription: {1}";

            /// <summary>
            /// Constant for dynamic message in EVException. This will also be in resource file.
            /// </summary>
            public const string EvCorlibError = "EvCorlib error (";
            public const string EvCorlibErrorDescription = "EvCorlib error description: ";
            public const string EvCorlibErrorNumber = "EvCorlib error number: ";
            public const string EvCorlibErrorSource = "EvCorlib error source: ";
            public const string OutputXmlFileName = "output.xml";
            public const string DateFormatString = "MM-dd-yyThh-mm-ss.ffff";
        }

        #endregion

        #region Fields and properties
        /// <summary>
        /// Session object for EVCorlib.
        /// </summary>
        private Session session;

        /// <summary>
        /// Configuration object for EVCorlib.
        /// </summary>
        private Config config;

        /// <summary>
        /// Temporary working directory for use of EVCorlib
        /// </summary>
        private readonly DirectoryInfo outputDirectory;

        /// <summary>
        /// Gets entry ids count. This might be used as number of atomic units while processing a mail store.
        /// </summary>
        private int entryIdsCount;

        private string passwordFile;

        private IEnumerable<string> passwords;

        /// <summary>
        /// Write Only - Sets passwords for extracting password protected archives
        /// </summary>
        public IEnumerable<string> Passwords
        {
            set
            {
                passwords = value;
                if (passwords != null && passwords.Any())
                {
                    passwordFile = CreatePasswordFile(passwords);
                }
            }
            get { return passwords; }
        }

        /// <summary>
        /// Output batch size, used while extracting mail stores 
        /// </summary>
        public int OutputBatchSize { get; set; }

        /// <summary>
        /// Gets temporary working directory object
        /// </summary>
        public DirectoryInfo OutputDirectory
        {
            get
            {
                return outputDirectory;
            }
        }

        /// <summary>
        /// Deletes temporary files created during document extraction
        /// </summary>
        public bool IsDeleteTemporaryFiles { get; set; }

        /// <summary>
        /// EvCorlib Session object
        /// </summary>
        public Session Session
        {
            get { return session; }
            set { session = value; }
        }

        /// <summary>
        /// Gets or Sets OutputFileName generated while extraction
        /// </summary>
        public string EdrmOutputFileName { get; set; }

        /// <summary>
        /// Gets entry ids count. This might be used as number of atomic units while processing a mail store.
        /// </summary>
        /// <value>
        /// The entry ids count.
        /// </value>
        public int EntryIdsCount
        {
            get
            {
                return entryIdsCount;
            }
        }

        #endregion

        #region Public functions and constructor

        /// <summary>
        /// Instantiates EVCorlib Manager with specified working directory
        /// </summary>
        /// <param name="outputDirectory"> Temporary working directory for file extraction </param>
        /// <param name="outputBatchSize"> Output batch size, used while extracting mail stores </param>
        public EvCorlibManager(DirectoryInfo outputDirectory, int outputBatchSize)
        {
            try
            {
                passwordFile = string.Empty;

                // By default delete all temporary files.
                IsDeleteTemporaryFiles = false;

                // output directory value provided by the user.
                this.outputDirectory = outputDirectory;

                // set default value - it need not be specified by consumer class.
                EdrmOutputFileName = Constants.OutputXmlFileName;

                // set output batch size - used while extracting e-mails
                OutputBatchSize = outputBatchSize;

            }
            catch (Exception exception)
            {
                exception.AddResMsg(ErrorCodes.EvCorlibManagerObjectCreateError);
                throw;
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void Initialize()
        {
            try
            {
                // EvCorLib create config object                
                config = new Config();

                // create EVCorlib session object
                session = new Session();

                // Set to load NTFS properties always
                config.LoadNtfsProps = true;

                config.Email_OutlookFormat = OutlookConversionFormats.Html;

                // set to load NTFS timestamps always
                config.LoadNtfsTimeStamps = true;

                // if password file is available, set it to EvCorlib Configuration.
                if (!string.IsNullOrWhiteSpace(passwordFile))
                {
                    config.PasswordFile = passwordFile;
                }

                // Use a temporary directory for EDLoader component to work with.
                string edLoaderTempDirectory = Path.Combine(config.ProcessTempDir, Guid.NewGuid().ToString());
                Directory.CreateDirectory(edLoaderTempDirectory);
                config.ProcessTempDir = edLoaderTempDirectory;

                // Initialize EVCorlib config object
                if (!session.Initialize(config))
                {
                    throw new EVException().AddUsrMsg(EvCorlibExceptionToString(session.LastException)).AddResMsg(ErrorCodes.EDLoaderSessionInitializeError);
                }
            }            
            catch (Exception exception)// Any generic error if occurred will be converted to EV Exception and thrown.
            {
                exception.AddResMsg(ErrorCodes.EVCorlibManagerInstantiateError);
                throw;
            }
        }

        /// <summary>
        /// Extracts file metadata and text.
        /// E-mail documents would have additional step to complete document extraction.
        /// </summary>
        /// <param name="files"> List of files being extracted as a batch </param>
        /// <returns> evCorlibEntity object encapsultes output - for normal documents it has extracted EDRM file location. For e-mails it has required information for next step. </returns>
        public EvCorlibEntity BatchProcessFiles(IEnumerable<FileInfo> files)
        {
            EvCorlibEntity evCorlibEntity = null;
            FileProcessor fileProcessor = null;

            evCorlibEntity = new EvCorlibEntity();
            fileProcessor = new FileProcessor();

            Initialize();

            if (files != null && files.Any())
            {
                List<string> fileNames = new List<string>();
                files.ToList().ForEach(p => fileNames.Add(p.FullName));

                if (fileNames.Count > 0)
                {
                    #region Process File Batch
                    if (fileProcessor.ProcessFileBatch(session, fileNames.ToArray(), outputDirectory.FullName, Guid.NewGuid().ToString()))
                    {
                        if (string.IsNullOrEmpty(fileProcessor.OutputFilename)) throw new EVException().AddErrorCode(ErrorCodes.NoOutpuFile);

                        #region Process File Batch Successfull

                        if (fileProcessor.OutputMailStoreCount > 0) // Check if it's a mail store.
                        {
                            evCorlibEntity.OutputFilePath = fileProcessor.OutputFilename;
                            evCorlibEntity.HasMailStores = true;

                            #region create mail stores and set them in evCorlib entity being returned
                            foreach (string store in fileProcessor.OutputMailstores.Cast<string>())
                            {
                                // Create Mail store object
                                MailStoresEntity mailStoresEntity = new MailStoresEntity();

                                // Determine mail store type.
                                if (store.StartsWith(Constants.OutlookPrefix))
                                {
                                    mailStoresEntity.EmailType = EmailType.Outlook;

                                    // prefix need to be removed to get to absolute path of mail store
                                    mailStoresEntity.MailStorePath = store.Replace(Constants.OutlookPrefix, string.Empty);
                                }
                                else if (store.StartsWith(Constants.LotusPrefix))
                                {
                                    mailStoresEntity.EmailType = EmailType.LotusNotes;

                                    // prefix need to be removed to get to absolute path of mail store
                                    mailStoresEntity.MailStorePath = store.Replace(Constants.LotusPrefix, string.Empty);
                                }
                                else throw new EVException().AddResMsg(ErrorCodes.UnsupportedMailStore);

                                evCorlibEntity.MailStoreEntity.Add(mailStoresEntity);
                            }
                            #endregion
                        }
                        else // no mail stores means that it's a stand alone file like a .doc or .xls
                        {
                            evCorlibEntity.HasMailStores = false;
                            evCorlibEntity.OutputFilePath = fileProcessor.OutputFilename;
                            evCorlibEntity.MailStoreEntity.Clear();
                        }
                        #endregion Process File Batch Successfull
                    }
                    else
                    {
                        #region Process File Batch Failure
                        StringBuilder exceptionDetail = new StringBuilder();

                        for (int counter = 0; counter < fileProcessor.Exceptions.Count; counter++)
                        {
                            #region build exception detail
                            ExceptionData exception = fileProcessor.Exceptions.ExceptionData(counter);
                            if (exception != null)
                            {
                                exceptionDetail.Append(Constants.EvCorlibError + counter + ")");
                                if (!string.IsNullOrEmpty(exception.Description)) exceptionDetail.Append(Constants.EvCorlibErrorDescription + exception.Description);
                                if (!string.IsNullOrEmpty(exception.Number.ToString(CultureInfo.InvariantCulture))) exceptionDetail.Append(Constants.EvCorlibErrorNumber + exception.Number.ToString(CultureInfo.InvariantCulture));
                                if (!string.IsNullOrEmpty(exception.Source)) exceptionDetail.Append(Constants.EvCorlibErrorSource + exception.Source);
                            }
                            #endregion
                        }
                        throw new EVException().AddResMsg(ErrorCodes.ProcessFileError).AddDbgMsg(exceptionDetail.ToString());
                        #endregion Process File Batch Failure
                    }

                    #endregion Process File Batch
                }
                else
                {
                    fileProcessor = null;
                    ((Exception)new EVException().AddErrorCode(ErrorCodes.NoFilesToProcess)).Trace().Swallow();
                }
            }
            else
            {
                fileProcessor = null;
                ((Exception)new EVException().AddErrorCode(ErrorCodes.NoFilesToProcess)).Trace().Swallow();
            }

            return evCorlibEntity;
        }

        /// <summary>
        /// Extracts mail stores, PSTs, NSFs etc.
        /// </summary>
        /// <param name="evcorlibEntity"></param>
        /// <param name="injectedFunction"> 
        /// Usage: Perform additional operations on documents as soon as extraction happens.
        /// This function might yield multiple documents, sometimes it's advisible to perform additional operations on documents as extractions happens.
        /// For example, if ProcessDocument extracts 100 documents, as each document is extracted, import should happen.
        /// To acheive this flexibility, a function can be injected (callback), which does the additional operation as extraction happens.
        /// </param>
        /// <returns> EDRM file locations. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IEnumerable<string> ProcessMailStores(EvCorlibEntity evcorlibEntity, Func<string, string, int?, object> injectedFunction)
        {
            try
            {
                List<string> outputEDRMFiles = new List<string>();
                // Verify if mail store are available to process
                if (evcorlibEntity.HasMailStores || (evcorlibEntity.MailStoreEntity != null && evcorlibEntity.MailStoreEntity.Count > 0))
                {
                    outputEDRMFiles = new List<string>();
                    // Loop through mail stores and process each of them.
                    foreach (MailStoresEntity mailStore in evcorlibEntity.MailStoreEntity)
                    {
                        List<string> entryIds = ListMailstore(mailStore.MailStorePath, mailStore.EmailType);

                        entryIdsCount = (entryIds != null)?entryIds.Count:0;

                        // extracts EDRM files using entry ids generated from each mail store
                        outputEDRMFiles.AddRange(BatchExtractMailStores(entryIds, mailStore.MailStorePath, mailStore.EmailType, injectedFunction));
                    }
                }
                else
                    throw new EVException().AddResMsg(ErrorCodes.NoMailStoresError);

                return outputEDRMFiles; // return output EDRML XMLs                
            }
            catch (EVException evException)
            {
                ((Exception)evException).Trace().Swallow();
            }
            catch (Exception exception)
            {
                exception.AddErrorCode(ErrorCodes.ProcessMailStoreError).Trace().Swallow();
            }

            return null;
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Converts EVCorLib exception data to string
        /// </summary>
        /// <param name="ex">EVCorLib Exception Data</param>
        /// <returns>Exception details as a string</returns>
        private string EvCorlibExceptionToString(ExceptionData ex)
        {
            if (ex.ExceptionClass == ExceptionClasses.EdCorlibProcessingException)
            {
                return string.Format(Constants.ExtractionLocationDirectoryName
                   , ex.ExceptionClass.ToString()
                   , ex.AssociatedInputUri
                   , ex.AssociatedUuid
                   , ex.Description);
            }
            else
            {
                return string.Format(Constants.EvCorlibExceptionDescription, ex.ExceptionClass.ToString(), ex.Description);
            }
        }

        /// <summary>
        /// Extracts mail stores in a mail file (example Inbox, sent items from PST)
        /// </summary>
        /// <param name="store"> Mail store name </param>
        /// <param name="emailType"> </param>
        /// <returns> List of mail store in a mail file (example Inbox, sent items from PST) </returns>
        private List<String> ListMailstore(string store, EmailType emailType)
        {
            MailStoreListProvider lister = null;
            MailStoreTypes storeType;

            try
            {
                storeType = ConvertEMailTypeToEVCorlibMailStoreType(emailType);

                lister = new MailStoreListProvider();
                StoreListHandler listHandler = new StoreListHandler();
                lister.CallbackBatchSize = 500;
                lister.StartListing(session, store, storeType, listHandler);

                if (listHandler.Complete && !listHandler.Aborted)
                {
                    return listHandler.EntryIds;
                }
                else
                {
                    throw new EVException().AddResMsg(ErrorCodes.EDLoaderListMailStoreError).AddDbgMsg(EvCorlibExceptionToString(listHandler.LastException));
                }
            }
            catch (EVException ex)
            {
                ((Exception)ex).Trace().Swallow();
            }
            catch (Exception ex)
            {
                ex.AddErrorCode(ErrorCodes.EDLoaderListMailStoreError).Trace().Swallow();
            }
            finally
            {
                if (lister != null) lister.Cleanup();
                lister = null;
            }

            return null;
        }

        /// <summary>
        /// Batches the extract mail stores.
        /// </summary>
        /// <param name="entryIds">The entry ids.</param>
        /// <param name="path">The path.</param>
        /// <param name="emailType">Type of the email.</param>
        /// <param name="injectedFunction"> </param>
        /// <returns></returns>
        private IEnumerable<string> BatchExtractMailStores(IEnumerable<string> entryIds, string path, EmailType emailType, Func<string, string, int?, object> injectedFunction)
        {
            MailProcessor mailProcessor = null;
            MailStoreTypes type;
            List<string> allOutputFiles = null;
            Func<IEnumerable<string>, string> ProcessEmailBatch = null;
            int lastEntryIdIndex = 0;
            string emailExtractionFolderName = string.Empty;

            try
            {

                allOutputFiles = new List<string>();
                if (entryIds == null || !entryIds.Any()) throw new EVException().AddErrorCode(ErrorCodes.EntryIDsUnavailable);

                // Convert emailtype to EVCOrlib specific type EMailStoreType
                type = ConvertEMailTypeToEVCorlibMailStoreType(emailType);

                // Create Mail Process Object
                mailProcessor = new MailProcessor();

                // initialize the mail processor
                if (!mailProcessor.Initialized)
                {
                    if (!mailProcessor.Initialize(session, path, type))
                    {
                        mailProcessor.Cleanup();
                        throw new EVException().AddResMsg(ErrorCodes.MailProcessorInitializeError);
                    }
                }

                emailExtractionFolderName = Path.Combine(OutputDirectory.FullName, Guid.NewGuid().ToString());

                #region Delegate to extract email batch

                // Delegate is created because there are two places within the function same code need to be copied.
                // Not a separate function as this is not usefull outside scope of this function.

                ProcessEmailBatch = delegate(IEnumerable<string> entryIds2)
                {
                    //get batch name
                    // string batchName = string.Format("{0}-{1}", lastEntryIdIndex - (OutputBatchSize - 1), lastEntryIdIndex);
                    // lLastEntryIdIndex 0 based index. ProcessEmailBatch used it for unique folder name.
                    // Idea is to have folder name as the index of first item in the list.
                    if (mailProcessor.ProcessEmailBatch(entryIds2.ToArray(), emailExtractionFolderName, lastEntryIdIndex.ToString(CultureInfo.InvariantCulture)))
                    {
                        // call injected function so that any activity like import would continue for extracted documents
                        injectedFunction(Path.Combine(emailExtractionFolderName, lastEntryIdIndex.ToString(CultureInfo.InvariantCulture), EdrmOutputFileName), path, lastEntryIdIndex);
                        // return e-mail output XML extracted.
                        return Path.Combine(emailExtractionFolderName, EdrmOutputFileName);
                    }
                    else
                    {
                        throw new EVException().AddResMsg(ErrorCodes.EmailMessageExtractionError);
                    }
                };

                // process all entry Ids as a batch
                if (entryIds.Count() > OutputBatchSize)
                {
                    List<string> entryIdList = entryIds.ToList();
                    do
                    {
                        lastEntryIdIndex += OutputBatchSize;

                        if (entryIdList.Count < OutputBatchSize) // for last set, when remaining items are less than output batch size, process and remove from the list.
                        {
                            allOutputFiles.Add(ProcessEmailBatch(entryIdList));
                            entryIdList.RemoveRange(0, entryIdList.Count);
                        }
                        else
                        {
                            allOutputFiles.Add(ProcessEmailBatch(entryIdList.Take(OutputBatchSize)));
                            entryIdList.RemoveRange(0, OutputBatchSize);
                        }
                    } while (entryIdList.Count > 0);
                }
                else
                {
                    lastEntryIdIndex += entryIds.Count();
                    allOutputFiles.Add(ProcessEmailBatch(entryIds));
                }
                #endregion Delegate to extract email batch

            }
            catch (EVException exception)
            {
                ((Exception)exception).Trace().Swallow();
            }
            catch (Exception exception)
            {
                string errorCode = ErrorCodes.EmailMessageExtractionError;
                exception.AddErrorCode(errorCode).Trace().Swallow();
            }
            finally
            {
                if (null != session)
                {
                    if (mailProcessor != null) mailProcessor.Cleanup();
                    session.CleanTemp();
                }
            }

            return allOutputFiles;
        }

        /// <summary>
        /// Convert EmailType to EVCorlib specific EMailStore type
        /// </summary>
        /// <param name="eMailType"> EmailType to be converted </param>
        /// <returns> EVCorlib specific EMailStore type </returns>
        private MailStoreTypes ConvertEMailTypeToEVCorlibMailStoreType(EmailType eMailType)
        {
            try
            {
                MailStoreTypes storeType;
                if (eMailType == EmailType.Outlook) storeType = MailStoreTypes.OutlookStore;
                else if (eMailType == EmailType.LotusNotes) storeType = MailStoreTypes.LotusStore;
                else throw new EVException().AddResMsg(ErrorCodes.UnsupportedMailStore);

                return storeType;
            }
            catch (EVException ex)
            {
                ((Exception)ex).Trace().Swallow();
            }
            catch (Exception ex)
            {
                string errorCode = ErrorCodes.MailStoreConversionError;
                ex.AddErrorCode(errorCode).Trace().Swallow();
            }
            return 0;
        }

        /// <summary>
        /// Creates password file, writes passwords  and returns password file name
        /// </summary>
        /// <returns> Password File URI </returns>
        private string CreatePasswordFile(IEnumerable<string> passwords)
        {
            TextWriter sWriter = null;
            string passwordFile = string.Empty;

            try
            {
                passwordFile = string.Format("{0}\\{1}.txt", outputDirectory.FullName, DateTime.UtcNow.ToString(Constants.DateFormatString));

                sWriter = new StreamWriter(passwordFile);
                foreach (string password in passwords)
                {
                    sWriter.WriteLine(password);
                }

                return passwordFile;
            }
            catch (Exception ex)
            {
                string errorCode = ErrorCodes.PasswordFileCreateError;
                ex.AddErrorCode(errorCode).Trace().Swallow();
            }
            finally
            {
                if (sWriter != null)
                {
                    sWriter.Close();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Determines whether the specified directory has documents recursively.
        /// </summary>
        /// <param name="dir">The directory to check.</param>
        /// <param name="exceptionList">The exception list. File names specified here are deleted</param>
        /// <returns>
        ///   <c>true</c> if the specified directory has documents; otherwise, <c>false</c>.
        /// </returns>
        private bool HasDocuments(DirectoryInfo dir, IEnumerable<string> exceptionList)
        {
            if (dir.GetDirectories().Any(subdir => HasDocuments(subdir, exceptionList)))
            {
                return true;
            }

            IEnumerable<FileInfo> files = dir.GetFiles();

            if (files.Any())
            {
                files = CleanExceptionFiles(files, exceptionList);
                return files != null && files.Any();
            }
            else
                return false;
        }

        /// <summary>
        /// Cleans the exception files. Matches a file by it's name
        /// </summary>
        /// <param name="pFiles">All files found in a directory.</param>
        /// <param name="exceptionList">files names to be deleted.</param>
        /// <returns>Remaining files that are not specified in exception list.</returns>
        private IEnumerable<FileInfo> CleanExceptionFiles(IEnumerable<FileInfo> pFiles, IEnumerable<string> exceptionList)
        {

            if (exceptionList == null)
                return null;

            List<FileInfo> files = pFiles.ToList();

            foreach (string exception in exceptionList)
            {
                IEnumerable<FileInfo> file = files.Where(p => p.Name.ToLower().Equals(exception));
                if (file != null && file.Any())
                {
                    file.First().Delete();
                    files.Remove(file.First());
                }
            }
            return files;
        }

        #endregion

        #region IDisposable Members
        /// <summary>
        /// Clean-up class level evcorlib objects.
        /// </summary>
        public void Dispose()
        {
            Cleanup();
        }

        #endregion

        /// <summary>
        /// Cleanups this instance.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.GC.Collect")]
        public void Cleanup()
        {
            try
            {
                if (session != null)
                {
                    session.CleanTemp();
                    session = null;
                }

                // Delete temporary password file
                if (!string.IsNullOrEmpty(passwordFile))
                {
                    FileInfo file = new FileInfo(passwordFile);
                    if (file.Exists) file.Delete();
                }
                config = null;

                // if delete temporary files is set to true, deletes all temporarily created files and directories
                if (IsDeleteTemporaryFiles)
                {
                    GC.Collect();

                    // if there are no documents (excluding output.xmls) delete the folder.
                    if (!HasDocuments(outputDirectory, new List<string> { "output.xml" }))
                        outputDirectory.Delete(true);

                    // Miscellaneous file delete 
                    DirectoryInfo folder = new DirectoryInfo(string.Format("{0}\\{1}", outputDirectory.FullName, "doc"));
                    if (folder.Exists)
                    {
                        if (!HasDocuments(folder, new List<string> { "output.xml" }))
                        {
                            folder.Delete(true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.AddErrorCode(ErrorCodes.CleanupFailure).Trace().Swallow();
            }
        }
    }
}
