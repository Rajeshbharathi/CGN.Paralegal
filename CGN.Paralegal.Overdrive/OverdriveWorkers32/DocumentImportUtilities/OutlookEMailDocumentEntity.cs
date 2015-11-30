
namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    public class OutlookEMailDocumentEntity : EmailDocumentEntity
    {
        public string ConversationTopic
        {
            get;
            set;
        }

        public string EmailClient
        {
            get;
            set;
        }

        public string InReplyToID
        {
            get;
            set;
        }

        public string ConversationIndex
        {
            get;
            set;
        }

        public string MessageClass
        {
            get;
            set;
        }

        public string SecurityFlags
        {
            get;
            set;
        }

        public string Sensitivity
        {
            get;
            set;
        }

        public string EntryId
        {
            get;
            set;
        }
    }
}
