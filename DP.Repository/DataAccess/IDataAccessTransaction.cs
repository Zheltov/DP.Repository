using System;

namespace DP.Repository.DataAccess
{
    /// <summary>
    /// DataAccess Transaction
    /// </summary>
    public interface IDataAccessTransaction : IDisposable
    {
        /// <summary>
        /// Parent Transaction that begin this transaction
        /// </summary>
        Transaction ParentTransaction { get; }
        /// <summary>
        /// Transaction State
        /// </summary>
        TransactionState State { get; }



        /// <summary>
        /// Commit transaction
        /// </summary>
        void Commit();
        /// <summary>
        /// Rollback transaction
        /// </summary>
        void Rollback();
    }
}