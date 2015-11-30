using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    /// <summary>
    /// Manages extracted information from Lotus Notes .nsf file (Deserialize EDRM and obtain data in usable format)
    /// </summary>
    public class LotusNotesEdrmManager : EmailDocumentManager
    {

        /// <summary>
        /// Delegate let's DocumentId be set by calling module per it's logic
        /// </summary>
        internal Func<string, string> CreateDocumentId;

        /// <summary>
        /// Gets or Sets Lotus Notes E-mail Document entities.
        /// </summary>
        public IEnumerable<LotusNotesEMailDocumentEntity> LotusNotesEmailDocuments { get; set; }

        /// <summary>
        /// Constants specific Lotus Notes EDRM Manager
        /// </summary>
        public class LotusNotesEdrmManagerConstants
        {
            /// <summary>
            /// Tag name for Conversation Topic in EDRM file
            /// </summary>
            public const string TagNameConversationTopic = "ConversationTopic";

            /// <summary>
            /// Tag name for Email Client in EDRM file
            /// </summary>
            public const string TagNameEmailClient = "EmailClient";

            /// <summary>
            /// Tag name for In-Reply-To Ide in EDRM file
            /// </summary>
            public const string TagNameInReplyToId = "";

            /// <summary>
            /// Tag Name for Email Document ID in EDRM file
            /// </summary>
            public const string TagNameLotusNotesEmailDocumentId = "";

            /// <summary>
            /// Tag name for Mail Store in EDRM file
            /// </summary>
            public const string TagNameMailStore = "MailStore";

            /// <summary>
            /// Tag name for reference id in EDRM file
            /// </summary>
            public const string TagNameReferenceId = "ReferenceID";

            /// <summary>
            /// Tag name for Lotus Notes message id
            /// </summary>
            public const string TagNameMessageId = "MsgId";

        }

        /// <summary>
        /// Error codes specific to Lotus Notes EDRM manager
        /// </summary>
        public class LotusNotesEdrmManagerErrorCodes
        {
            /// <summary>
            /// Represents failure to create Lotus Notes Edrm Manager object
            /// </summary>
            public const string CreateLotusNotesEdrmManagerFailure = "CreateLotusNotesEdrmManagerFailure";
        }

        /// <summary>
        /// Instantiates Lotus Notes EDRM Manager
        /// </summary>
        /// <param name="edrmFile"></param>
        public LotusNotesEdrmManager(string edrmFile)
            : base(edrmFile)
        {
            try 
            {               
                LotusNotesEmailDocuments = InitializeLotusNotesEDRMEntities(EmailDocumentEntities);
            }
            catch (EVException evException)
            {
                ((Exception)evException).Trace().Swallow();
            }
            catch (Exception exception)
            {
                string errorCode = LotusNotesEdrmManagerErrorCodes.CreateLotusNotesEdrmManagerFailure;
                exception.AddErrorCode(errorCode).Trace().Swallow();
            }
        }

        /// <summary>
        /// If the document is a lotus notes e-mail generate and return document id using EDRM Document Id and Threading Constraint value. Otherwise return document id as is
        /// It uses delegate - if null, uses original document id.
        /// </summary>
        /// <param name="document"> EDRM Document Entity </param>
        /// <returns> conversation index or original document id </returns>
        protected override string GetDocumentId(DocumentEntity document)
        {
            // Create Document Id from Doc Id value of Lotus Notes e-mail message
            if (document != null && (!string.IsNullOrEmpty(document.DocumentID)) && CreateDocumentId != null) return CreateDocumentId(document.DocumentID);
            else return base.GetDocumentId(document);
        }

        /// <summary>
        /// Create LotusNotesEmailDocumentObject using deserialized object's data
        /// </summary>
        /// <param name="documentEntities"> Document Entity to be transformed </param>
        /// <returns> list of lotus notes email documents </returns>
        private IEnumerable<LotusNotesEMailDocumentEntity> InitializeLotusNotesEDRMEntities(IEnumerable<EmailDocumentEntity> documentEntities)
        {
            List<LotusNotesEMailDocumentEntity> lotusNotesEmailDocumentEntities = new List<LotusNotesEMailDocumentEntity>();
            TagEntity tagEntity = null;

            foreach (EmailDocumentEntity document in documentEntities)
            {
                LotusNotesEMailDocumentEntity lotusNotesEmailDocumentEntity = TransformEmailDocumentEntityToLotusNotesEmailDocumentEntity(document);

                #region Unused logical properties - use then when required

                /*
                * Below are logical properties in a Outlook email document entity - they are not needed in current system, hence to improve performance commenting out this code.
                * This shall be used need basis
                 * 
                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(LotusNotesEdrmManagerConstants.TagNameEmailClient));
                lotusNotesEmailDocumentEntity.EmailClient = (tagEntity != null) ? tagEntity.TagValue.ToString() : string.Empty;
                */
                #endregion

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(LotusNotesEdrmManagerConstants.TagNameConversationTopic));
                lotusNotesEmailDocumentEntity.ConversationTopic = (tagEntity != null) ? tagEntity.TagValue : string.Empty;

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(LotusNotesEdrmManagerConstants.TagNameInReplyToId));
                lotusNotesEmailDocumentEntity.InReplyToId = (tagEntity != null) ? tagEntity.TagValue : string.Empty;

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(LotusNotesEdrmManagerConstants.TagNameLotusNotesEmailDocumentId));
                lotusNotesEmailDocumentEntity.LotusNotesEmailDocumentID = (tagEntity != null) ? tagEntity.TagValue : string.Empty;

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(LotusNotesEdrmManagerConstants.TagNameMailStore));
                lotusNotesEmailDocumentEntity.MailStore = (tagEntity != null) ? tagEntity.TagValue : string.Empty;

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(LotusNotesEdrmManagerConstants.TagNameReferenceId));
                lotusNotesEmailDocumentEntity.ReferenceId = (tagEntity != null) ? tagEntity.TagValue : string.Empty;

                tagEntity = document.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(LotusNotesEdrmManagerConstants.TagNameMessageId));
                lotusNotesEmailDocumentEntity.MessageID = (tagEntity != null) ? tagEntity.TagValue : string.Empty;

                lotusNotesEmailDocumentEntities.Add(lotusNotesEmailDocumentEntity);
            }

            return lotusNotesEmailDocumentEntities;
        }

        /// <summary>
        /// Transform data from base class to derved class entity.
        /// </summary>
        /// <param name="emailDocument"> base class entity emailDocumentEntity to be transformed </param>
        /// <returns> final lotus notes email document entity </returns>
        private LotusNotesEMailDocumentEntity TransformEmailDocumentEntityToLotusNotesEmailDocumentEntity(EmailDocumentEntity emailDocument)
        {
            LotusNotesEMailDocumentEntity lotusNotesEmailDocumentEntity = new LotusNotesEMailDocumentEntity
            {
                DocumentID = emailDocument.DocumentID,
                MIMEType = emailDocument.MIMEType
            };

            lotusNotesEmailDocumentEntity.Files.AddRange(emailDocument.Files);
            lotusNotesEmailDocumentEntity.Tags.AddRange(emailDocument.Tags);
            lotusNotesEmailDocumentEntity.MessageID = emailDocument.MessageID;
           
            #region Unused logical properties - use them when required.                
            /*
            * Below are logical properties in a email document entity - they are not needed in current system, hence to improve performance commenting out this code.
            * This shall be used need basis

            lotusNotesEmailDocumentEntity.DateReceived = emailDocument.DateReceived;
            lotusNotesEmailDocumentEntity.DateSent = emailDocument.DateSent;
            lotusNotesEmailDocumentEntity.From = emailDocument.From;
            lotusNotesEmailDocumentEntity.IsDeliveryReceiptSet = emailDocument.IsDeliveryReceiptSet;
            lotusNotesEmailDocumentEntity.IsReadReceiptSet = emailDocument.IsReadReceiptSet;            
            lotusNotesEmailDocumentEntity.Subject = emailDocument.Subject;
            lotusNotesEmailDocumentEntity.To = emailDocument.To;
           */
            #endregion

            return lotusNotesEmailDocumentEntity;
        }
    }
}
