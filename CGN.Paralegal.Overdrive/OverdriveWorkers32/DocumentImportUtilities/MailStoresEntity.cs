
namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    public class MailStoresEntity
    {

        /// <summary>
        /// Get or Set mailstores that need extracted from a mail file, for example .pst file or .nsf
        /// </summary>
        public string MailStorePath
        {
            get;
            set;
        }

        /// <summary>
        /// Type of e-mail document being processed (Outlook, Lotus Notes etc.)
        /// </summary>
        public EmailType EmailType
        {
            get;
            set;
        }
    }
}
