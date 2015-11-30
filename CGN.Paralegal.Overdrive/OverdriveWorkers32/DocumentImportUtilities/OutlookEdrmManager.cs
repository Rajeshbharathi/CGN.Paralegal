using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    /// <summary>
    /// EDRM XML file Manager for outlook E-mail Documents
    /// </summary>
    public class OutlookEdrmManager : EmailDocumentManager
    {
        /// <summary>
        /// Constants specific to Outlook EDRM Manager class
        /// </summary>
        public class OutlookEdrmManagerConstants
        {
            /// <summary>
            /// Tag Name representing DateReceived in EDRM file
            /// </summary>
            public const string TagNameConversationIndex = "ConversationIndex";

            /// <summary>
            /// Tag Name representing ConversationTopic in EDRM file
            /// </summary>
            public const string TagNameConversationTopic = "ConversationTopic";

            /// <summary>
            /// Tag Name representing EmailClient in EDRM file
            /// </summary>
            public const string TagNameEmailClient = "EmailClient";

            /// <summary>
            /// Tag Name representing InReplyToId in EDRM file
            /// </summary>
            public const string TagNameInReplyToId = "InReplyToId";

            /// <summary>
            /// Tag Name representing MessageClass in EDRM file
            /// </summary>
            public const string TagNameMessageClass = "#MessageClass";

            /// <summary>
            /// Tag Name representing SecurityFlags in EDRM file
            /// </summary>
            public const string TagNameSecurityFlags = "SecurityFlags";

            /// <summary>
            /// Tag Name representing Sensitivity in EDRM file
            /// </summary>
            public const string TagNameSensitivity = "Sensitivity";

            /// <summary>
            /// Tag Name representing Entry Id for an Outlook email message.
            /// </summary>
            public const string TagNameEntryId = "EntryId";

        }

        /// <summary>
        /// Error codes specific to OutlookEdrmManager
        /// </summary>
        public class OutlookEdrmManagerErrorCodes
        {

            /// <summary>
            /// Represents error in constructor - failed to create object of OutlookEdrmManager
            /// </summary>
            public const string CreateOutlookEdrmManagerFailure = "CreateOutlookEdrmManagerFailure";
        }

        /// <summary>
        /// Delegate let's DocumentId be set by calling module per it's logic
        /// </summary>
        internal Func<string, string> GetDocumentIdFromConversationIndex;

        /// <summary>
        /// Gets or Sets outlook email document entities
        /// </summary>
        public IEnumerable<OutlookEMailDocumentEntity> OutlookEmailDocumentEntities { get; set; }

        /// <summary>
        /// Initializes outlook EDRM Manager
        /// </summary>
        /// <param name="edrmFile"> EDRM file using which the Outlook objects are created </param>
        public OutlookEdrmManager(string edrmFile) : base(edrmFile) 
        {
            try
            {
                OutlookEmailDocumentEntities = InitializeOutlookEdrmEntities(EmailDocumentEntities);                
            }
            catch (EVException evException)
            {
                ((Exception)evException).Trace().Swallow();
            }
            catch (Exception exception)
            {
                string errorCode = OutlookEdrmManagerErrorCodes.CreateOutlookEdrmManagerFailure;
                exception.AddErrorCode(errorCode).Trace().Swallow();
            }
        }

        /// <summary>
        /// If the document is a e-mail in PST file generate and return document id using Conversation Index and Threading Constraint value. Otherwise return document id as is
        /// It uses delegate - if null, uses original document id.
        /// </summary>
        /// <param name="document"> EDRM Document Entity </param>
        /// <returns> conversation index or original document id </returns>
        protected override string GetDocumentId(DocumentEntity document)
        {
            TagEntity tag = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.Equals(OutlookEdrmManagerConstants.TagNameConversationIndex));
           
            if (tag != null && GetDocumentIdFromConversationIndex != null) return  GetDocumentIdFromConversationIndex(tag.TagValue);
            return base.GetDocumentId(document);
        }

        /// <summary>
        /// Create OutlookEmailDocumentObject using deserialized object's data
        /// </summary>
        /// <param name="documentEntities"> Document Entity to be transformed </param>
        /// <returns> list of outlook email documents </returns>
        private IEnumerable<OutlookEMailDocumentEntity> InitializeOutlookEdrmEntities(IEnumerable<EmailDocumentEntity> documentEntities)
        {
            List<OutlookEMailDocumentEntity> outlookEmailDocumentEntities = new List<OutlookEMailDocumentEntity>();

            foreach (EmailDocumentEntity document in documentEntities)
            {                
                OutlookEMailDocumentEntity outlookEmailDocument = TransformEmailDocumentEntityToOutlookEMailDocument(document);

                #region Unused logical properties - use them when required.
                /*
                * Below are logical properties in a Outlook email document entity - they are not needed in current system, hence to improve performance commenting out this code.
                * This shall be used need basis

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(constants.TagNameEmailClient));
                outlookEmailDocument.EmailClient = (tagEntity != null) ? tagEntity.TagValue.ToString() : string.Empty;
               
                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(constants.TagNameMessageClass));
                outlookEmailDocument.MessageClass = (tagEntity != null) ? tagEntity.TagValue.ToString() : string.Empty; 
                
                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(constants.TagNameSecurityFlags));
                outlookEmailDocument.SecurityFlags = (tagEntity != null) ? tagEntity.TagValue.ToString() : string.Empty;

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(constants.TagNameSensitivity));
                outlookEmailDocument.Sensitivity = (tagEntity != null) ? tagEntity.TagValue.ToString() : string.Empty;
                 * 
                */
                #endregion

                TagEntity tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(OutlookEdrmManagerConstants.TagNameConversationIndex));
                outlookEmailDocument.ConversationIndex = (tagEntity != null)? tagEntity.TagValue.ToString() : string.Empty;

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(OutlookEdrmManagerConstants.TagNameConversationTopic));
                outlookEmailDocument.ConversationTopic = (tagEntity != null) ? tagEntity.TagValue.ToString() : string.Empty;

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(OutlookEdrmManagerConstants.TagNameInReplyToId));
                outlookEmailDocument.InReplyToID = (tagEntity != null) ? tagEntity.TagValue.ToString() : string.Empty;

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(OutlookEdrmManagerConstants.TagNameEntryId));
                outlookEmailDocument.EntryId = (tagEntity != null) ? tagEntity.TagValue.ToString() : string.Empty;

                outlookEmailDocumentEntities.Add(outlookEmailDocument);
            }
            
            return outlookEmailDocumentEntities;
        }

        /// <summary>
        /// Transform data from base class to derved class entity.
        /// </summary>
        /// <param name="emailDocument"> base class entity emailDocumentEntity to be transformed </param>
        /// <returns> final outlook email document entity </returns>
        private static OutlookEMailDocumentEntity TransformEmailDocumentEntityToOutlookEMailDocument(EmailDocumentEntity emailDocument)
        {
            OutlookEMailDocumentEntity outlookEMailDocumentEntity = new OutlookEMailDocumentEntity
            {
                DocumentID = emailDocument.DocumentID,
                MIMEType = emailDocument.MIMEType,
            };

            outlookEMailDocumentEntity.Files.AddRange(emailDocument.Files);
            outlookEMailDocumentEntity.Tags.AddRange(emailDocument.Tags);
            outlookEMailDocumentEntity.MessageID = emailDocument.MessageID;

           #region Unused logical properties - use them when required.        
        
            /*
            * Below are logical properties in a email document entity - they are not needed in current system, hence to improve performance commenting out this code.
            * This shall be used need basis
            outlookEMailDocumentEntity.DateReceived = emailDocument.DateReceived;
            outlookEMailDocumentEntity.DateSent = emailDocument.DateSent;
            outlookEMailDocumentEntity.From = emailDocument.From;
            outlookEMailDocumentEntity.IsDeliveryReceiptSet = emailDocument.IsDeliveryReceiptSet;
            outlookEMailDocumentEntity.IsReadReceiptSet = emailDocument.IsReadReceiptSet;             
            outlookEMailDocumentEntity.Subject = emailDocument.Subject;
            outlookEMailDocumentEntity.To = emailDocument.To;
            
           */

           #endregion

            return outlookEMailDocumentEntity;
        }      
    }
}
