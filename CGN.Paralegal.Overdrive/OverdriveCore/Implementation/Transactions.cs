using System;
using System.Transactions;

namespace LexisNexis.Evolution.Overdrive
{
    public class OverdriveTransactionScope
    {
        public static TransactionScope CreateNew()
        {
            return new TransactionScope(TransactionScopeOption.RequiresNew, transactionOptions);
        }

        internal static TransactionScope CreateInherited()
        {
            return new TransactionScope(TransactionScopeOption.Required, transactionOptions);
        }

        public static TransactionScope Suppress()
        {
            return new TransactionScope(TransactionScopeOption.Suppress);
        }

        private static readonly TimeSpan timeout = TransactionManager.MaximumTimeout; // or new TimeSpan(1, 0, 0, 0);
        private readonly static TransactionOptions transactionOptions = 
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = timeout };
    }

    //public class OverdriveTransaction
    //{
    //    public void DoTransactionalWork(Transaction myTransaction, delegate work)
    //    {
    //        Transaction oldAmbient = Transaction.Current;
    //        Transaction.Current = myTransaction;

    //        try
    //        {
    //        }
    //        finally
    //        {
    //            //Restore the ambient transaction 
    //            Transaction.Current = oldAmbient;
    //        }
    //    }        
    //}
}
