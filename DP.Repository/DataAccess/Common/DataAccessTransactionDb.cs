using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository.DataAccess.Common
{
    /// <summary>
    /// DataAccess transaction
    /// </summary>
    public sealed class DataAccessTransactionDb : IDataAccessTransaction
    {
        /// <summary>
        /// Physical db transaction
        /// </summary>
        public DbTransaction DbTransaction
        {
            get
            {
                return Connection.DbTransaction;
            }
        }

        public Transaction ParentTransaction { get; private set; }

        public TransactionState State { get; private set; }

        /// <summary>
        /// DataAccessConnection
        /// </summary>
        public DataAccessConnectionDb Connection { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">DataAccessConnection that begin this transaction</param>
        internal DataAccessTransactionDb( DataAccessConnectionDb connection )
        {
            Connection = connection;
            State = TransactionState.Opened;

        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">DataAccessConnection that begin this transaction</param>
        /// <param name="parentTransaction">Parent Transaction in that created this transaction</param>
        internal DataAccessTransactionDb( DataAccessConnectionDb connection, Transaction parentTransaction )
            : this( connection )
        {
            ParentTransaction = parentTransaction;
        }
        ~DataAccessTransactionDb()
        {
            Dispose( false );
        }

        public void Commit()
        {
            if ( State != TransactionState.Opened )
                throw new TransactionException( "Transaction already has closed" );

            Connection.CommitTransaction( this );

            State = TransactionState.Commited;
        }

        public void Rollback()
        {
            if ( State == TransactionState.Rollbacked )
                return;

            State = TransactionState.Rollbacked;

            Connection.RollbackTransactions( this );
        }

        /// <summary>
        /// Implement IDispose
        /// </summary>
        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        void Dispose( bool disposing )
        {
            // unmanaged
            if ( State == TransactionState.Opened )
                Rollback();
        }
    }
}