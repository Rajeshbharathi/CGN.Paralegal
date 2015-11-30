using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    /// <summary>
    /// Factory class to create IMailProcessor objects. For example Outlook or Lotus Notes Mail Adapters
    /// </summary>
    public class MailProcessorFactory
    {
        /// <summary>
        /// Captures IDs for all errors thrown by the  MailProcessorFactory
        /// </summary>
        public class ErrorCodes
        {
            /// <summary>
            /// failure to create mail processor might be due to unknown file type or no mail store available.
            /// </summary>
            public const string MailProcessorCreateFailure = "MailProcessorCreateFailure";
        }

        /// <summary>
        /// Creates Mail Processor object based on mail store type
        /// </summary>
        /// <param name="evCorlibEntity"> evCorlibEntity is expected to have mail store details </param>
        /// <returns> object of class implementing IMailProcessor </returns>
        public IMailProcessor CreateMailProcessor(EvCorlibEntity evCorlibEntity)
        {
            if (evCorlibEntity != null && evCorlibEntity.HasMailStores && evCorlibEntity.MailStoreEntity.Count > 0)
            {
                if (evCorlibEntity.MailStoreEntity[0].EmailType == EmailType.Outlook) return new OutlookAdapter();
                if (evCorlibEntity.MailStoreEntity[0].EmailType == EmailType.LotusNotes) return new LotusNotesAdapater();
                throw new EVException().AddResMsg(ErrorCodes.MailProcessorCreateFailure);
            }
            throw new EVException().AddResMsg(ErrorCodes.MailProcessorCreateFailure);
        }
    }
}
