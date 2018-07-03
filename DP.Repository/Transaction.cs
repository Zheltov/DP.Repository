using DP.Repository.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository
{
    /// <summary>
    /// Repository level transaction.
    /// Started from Connection.BeginTransaction and it start transaction for all DataAccessConnections registered in Connection
    /// that create this transaction.
    /// </summary>
    public class Transaction : IDisposable
    {
        /// <summary>
        /// List of DataAccessTransaction started in this transaction
        /// </summary>
        IList<IDataAccessTransaction> dataAccessTransactions;

        /// <summary>
        /// Transaction state
        /// </summary>
        public TransactionState State { get; private set; }

        /// <summary>
        /// Connection
        /// </summary>
        public Connection Connection { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">Connection that begin this transaction</param>
        internal Transaction( Connection connection )
        {
            Connection = connection;
            dataAccessTransactions = new List<IDataAccessTransaction>();
            
            try
            {
                // Start transactions for all DataAccessConnections
                foreach ( var dataAccessTransactionManager in connection.DataAccessConnections )
                    dataAccessTransactions.Add( dataAccessTransactionManager.BeginTransaction( this ) );

                State = TransactionState.Opened;
            }
            catch ( Exception )
            {
                var dataAccessTransactionsToDispose = dataAccessTransactions;
                dataAccessTransactions.Clear();

                // Dispose all started transactions
                foreach ( var dataAccessTransaction in dataAccessTransactionsToDispose )
                    dataAccessTransaction.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Добавить MethodCallExpression которое будет выполнено
        /// при первом физическом коммите среди всех соединений в рамках единой транзакции
        /// Пример: 
        ///     AddTrigger( () => Context.RepositoryTrigger.UpdateOrderDynamic( order.OrderId ) );
        /// Выражение должно приводится к типу MethodCallExpression
        /// </summary>
        /// <param name="action">Выражение приводимое к типу MethodCallExpression</param>
        public void AddTrigger( Expression<Action> action )
        {
            Connection.AddTrigger( action );
        }

        /// <summary>
        /// Commit this transaction
        /// </summary>
        public void Commit()
        {
            if ( State == TransactionState.Commited )
                throw new TransactionException( "Transaction already has commited" );

            if ( State == TransactionState.Rollbacked )
                throw new TransactionException( "Transaction already has rollbacked" );

            // Commit all opened DataAccess Transactions             
            foreach ( var dataAccessTransaction in dataAccessTransactions )
                dataAccessTransaction.Commit();

            // Commit this transaction
            Connection.CommitTransaction( this );

            State = TransactionState.Commited;
        }

        /// <summary>
        /// Rollback this transaction
        /// </summary>
        public void Rollback()
        {
            if ( State == TransactionState.Rollbacked )
                return;

            // Rollback all opened DataAccess Transactions
            Exception firstException = null;
            foreach ( var dataAccessTransaction in dataAccessTransactions )
            {
                try
                {
                    dataAccessTransaction.Rollback();
                }
                catch ( Exception ex )
                {
                    if ( firstException == null )
                        firstException = ex;
                }
            }

            try
            {
                Connection.RollbackTransactions( this );
            }
            catch ( Exception ex )
            {
                if ( firstException == null )
                    firstException = ex;
            }

            State = TransactionState.Rollbacked;

            if ( firstException != null )
                throw new TransactionUnknowStateException( firstException.Message, firstException );

        }

        /// <summary>
        /// Implementation IDispose interface
        /// </summary>
        public void Dispose()
        {
            if ( State == TransactionState.Opened )
                Rollback();
        }
    }

    /// <summary>
    /// Transaction sate enum
    /// </summary>
    public enum TransactionState
    {
        Opened,
        Commited,
        Rollbacked
    }
}