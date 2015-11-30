using System;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    public class EmailDocumentEntity : DocumentEntity
    {
        public DateTime DateReceived
        {
            get;
            set;
        }

        public DateTime DateSent
        {
            get;
            set;
        }

        public string From
        {
            get;
            set;
        }

        public bool IsReadReceiptSet
        {
            get;
            set;
        }

        public bool IsDeliveryReceiptSet
        {
            get;
            set;
        }

        public string MessageID
        {
            get;
            set;
        }

        public string Subject
        {
            get;
            set;
        }

        public string To
        {
            get;
            set;
        }
    }
}
