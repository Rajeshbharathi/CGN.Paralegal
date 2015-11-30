using System;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DocumentImportUtilities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    using System.Globalization;

    /// <summary>
    /// EDRM XML file Manager for E-mail Documents
    /// </summary>
    public abstract class EmailDocumentManager : EDRMManager
    {
       
        /// <summary>
        /// Constants specific to e-mail document manager
        /// </summary>
        class Constants
        {

            private Constants() { }

            /// <summary>
            /// Tag Name representing MsgId in EDRM file
            /// </summary>
            public const string TagNameMessageId = "MsgId";
        }

        /// <summary>
        /// Errors specific to e-mail document manager
        /// </summary>
        public class EmailDocumentManagerErrorCodes
        {
            /// <summary>
            /// Error representing failure to create e-mail document entity
            /// </summary>
            public const string EmailDocumentEntititesCreateObjectFailure = "EmailDocumentEntititesCreateObjectFailure";
        }
        
        private readonly List<EmailDocumentEntity> emailDocumentEntities;

        /// <summary>
        /// Gets email document entities in the speicified edrm file. Use AddRange for set.
        /// </summary>
        public IEnumerable<EmailDocumentEntity> EmailDocumentEntities
        {
            get { return emailDocumentEntities; }            
        }        

        /// <summary>
        /// Initialize EMailDocument Manager object using e-mail EDRM file
        /// </summary>
        /// <param name="edrmFile"> email EDRM file with emetadata and reference to files </param>
        public EmailDocumentManager(string edrmFile) : base( edrmFile)
        {
            try
            {
                emailDocumentEntities = new List<EmailDocumentEntity>();
                emailDocumentEntities.AddRange(InitializeEmailDocumentEntity(EDRMEntity.BatchEntity.DocumentEntity));
            }
            catch (EVException evException)
            {
                ((Exception)evException).Trace().Swallow();
            }
            catch (Exception exception)
            {
                const string ErrorCode = EmailDocumentManagerErrorCodes.EmailDocumentEntititesCreateObjectFailure;
                exception.AddErrorCode(ErrorCode).Trace().Swallow();
            }
        }

        /// <summary>
        /// Obtain list of email document object specific properties from document entities. (Document entities are deserialized from EDRM XML)
        /// </summary>
        /// <param name="documentEntities">emailDocumentEntities</param>
        /// <returns> email document entities </returns>
        private IEnumerable<EmailDocumentEntity> InitializeEmailDocumentEntity(IEnumerable<DocumentEntity> documentEntities)
        {
            List<EmailDocumentEntity> emailDocumentEntities = new List<EmailDocumentEntity>();
            TagEntity tagEntity = null;
            foreach (DocumentEntity documentEntity in documentEntities)
            {
                EmailDocumentEntity emailDocument = ConvertDocumentEntityToEmailDocumentEntity(documentEntity);

                #region Unused logical properties - use them when required.                
                /*
                 * Below are logical properties in a email document entity - they are not needed in current system, hence to improve performance commenting out this code.
                 * This shall be used need basis
                 * 
                tagEntity = documentEntity.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(Constants.TagNameDateReceived));
                emailDocument.DateReceived = (tagEntity != null)? (DateTime)ConvertTypeAndSuppressError(tagEntity.TagValue, typeof(DateTime)):DateTime.MinValue;

                tagEntity = documentEntity.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(Constants.TagNameDateSent));
                emailDocument.DateSent = (tagEntity != null)?(DateTime)ConvertTypeAndSuppressError(tagEntity.TagValue, typeof(DateTime)):DateTime.MinValue;

                tagEntity = documentEntity.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(Constants.TagNameFrom));
                emailDocument.From =(tagEntity != null)? tagEntity.TagValue.ToString() : string.Empty;

                tagEntity = documentEntity.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(Constants.TagNameIsDeliveryReceiptSet));
                emailDocument.IsDeliveryReceiptSet = (tagEntity != null)?(Boolean)ConvertTypeAndSuppressError(tagEntity.TagValue.ToString(), typeof(Boolean)):false;

                tagEntity = documentEntity.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(Constants.TagNameIsReadReceiptSet));
                emailDocument.IsReadReceiptSet = (tagEntity != null)?(Boolean)ConvertTypeAndSuppressError(tagEntity.TagValue.ToString(), typeof(Boolean)):false;                

                tagEntity = documentEntity.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(Constants.TagNameSubject));
                emailDocument.Subject = (tagEntity != null)?tagEntity.TagValue.ToString():string.Empty;

                tagEntity = documentEntity.Tags.FirstOrDefault<TagEntity>(p => p.TagName.ToString().Equals(Constants.TagNameTo));
                emailDocument.To = (tagEntity != null)?tagEntity.TagValue.ToString() : string.Empty;
                 */
                #endregion

                tagEntity = documentEntity.Tags.FirstOrDefault(p => p.TagName.ToString(CultureInfo.InvariantCulture).Equals(Constants.TagNameMessageId));
                emailDocument.MessageID = (tagEntity != null) ? tagEntity.TagValue : string.Empty;

                emailDocumentEntities.Add(emailDocument);
            }
            return emailDocumentEntities;            
        }

        /// <summary>
        /// Create an EmailDocumentEntity object based on DocumentEntity object
        /// </summary>
        /// <param name="documentEntity"> Document Entity object to be converted</param>
        /// <returns> Converted EmailDocumentEntity </returns>
        private static EmailDocumentEntity ConvertDocumentEntityToEmailDocumentEntity(DocumentEntity documentEntity)
        {
            EmailDocumentEntity emailDocumentEntity = new EmailDocumentEntity
            {
                DocumentID = documentEntity.DocumentID,
                MIMEType= documentEntity.MIMEType,                
            };

            emailDocumentEntity.Files.AddRange(documentEntity.Files);
            emailDocumentEntity.Tags.AddRange(documentEntity.Tags);

            return emailDocumentEntity;
        }
    }
}
