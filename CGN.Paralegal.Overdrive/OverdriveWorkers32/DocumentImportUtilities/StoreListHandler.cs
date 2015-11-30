using System;
using System.Collections.Generic;
using System.Linq;
using evcorlib;


namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    /// <summary>
    /// Encapsulates the functionality to handle mail stores and managing entry Ids.
    /// Warning: This class is closely tied to EVCorlib COM DLL and provided by LAW team. Refrain from making changes.
    /// </summary>
    class StoreListHandler : IMailStoreListHandler
    {
        public List<string> EntryIds { get; set; }
        public ExceptionData LastException { get; set; }
        public bool Aborted { get; set; }
        public bool Complete { get; set; }

        /// <summary>
        /// Instantiate Store List Hanlder object
        /// </summary>
        public StoreListHandler()
        {
            Aborted = false;
            LastException = null;
            EntryIds = new List<string>();
            Complete = false;
        }


        #region _IMailStoreListHandler Members

        public void Listing(ref Array entryIds, int length, int total, ref bool cancel)
        {
            // uuuugh
            EntryIds.AddRange(entryIds.Cast<string>());
        }

        public void ListingAborted(ref ExceptionData ex)
        {
            Aborted = true;
            LastException = ex;
        }

        public void ListingCompleted(int total, bool aborted, bool cancelled)
        {
            Complete = true;
            Aborted = aborted;
        }

        public void ListingInitializationFailure(ref ExceptionData ex)
        {
            Complete = true;
            Aborted = true;
            LastException = ex;

        }

        public void ListingStarted(string mailStore)
        {
            Aborted = false;
            Complete = false;
            LastException = null;
            EntryIds.Clear();

        }

        #endregion
    }
}
