
namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    public class LotusNotesEMailDocumentEntity : EmailDocumentEntity
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

        public string InReplyToId
        {
            get;
            set;
        }

        public string LotusNotesEmailDocumentID
        {
            get;
            set;
        }

        public string MailStore
        {
            get;
            set;
        }

        public string ReferenceId
        {
            get;
            set;
        }
    }
}
