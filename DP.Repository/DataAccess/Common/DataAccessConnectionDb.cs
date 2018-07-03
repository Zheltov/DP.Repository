using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository.DataAccess.Common
{
    /// <summary>
    /// Common implementation DataAccessConnection based on standard infrastructure of System.Data.Common.
    /// Can be used for all standard Db Providers also as: MsSql, Npgsql, SQLite, etc
    /// This connection use deferred transaction. Then we call BeginTransaction() the real db transaction did not
    /// start. They real start then we call property DbTransaction. 
    /// </summary>
    public sealed class DataAccessConnectionDb : IDataAccessConnection
    {
        DbProviderFactory dbProviderFactory;
        Stack<DataAccessTransactionDb> transactions;
        DbTransaction dbTransaction;

        /// <summary>
        /// Get real db transaction.
        /// If transaction not exists (deferred), they created.
        /// </summary>
        internal DbTransaction DbTransaction
        {
            get
            {
                CheckState();

                if ( TransactionsCount == 0 )
                    throw new TransactionUnknowStateException(
                        string.Format( "Unkonow state. DataAccessConnectionDb of type [{0}] for providerName = [{1}] has 0 started transactions, but called create physical transaction",
                            GetType().Name,
                            ProviderName ) );

                if ( dbTransaction == null )
                {
                    // Start new transaction
                    DbConnection connection = dbProviderFactory.CreateConnection();
                    try
                    {
                        connection.ConnectionString = ConnectionString;
                        connection.Open();
                        dbTransaction = connection.BeginTransaction();
                    }
                    catch ( Exception )
                    {
                        if ( connection != null )
                        {
                            connection.Close();
                            connection.Dispose();
                        }
                        throw;
                    }
                }

                return dbTransaction;
            }
        }

        /// <summary>
        /// Connection string
        /// </summary>
        public string ConnectionString { get; private set; }
        /// <summary>
        /// Db provider name for resolve DbProviderFactories.GetFactory
        /// </summary>
        public string ProviderName { get; private set; }
        /// <summary>
        /// Connection Name
        /// </summary>
        public string Name { get; set; }

        public int TransactionsCount
        {
            get
            {
                return transactions.Count;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">ConnectionStringSettings</param>
        public DataAccessConnectionDb( ConnectionStringSettings connectionString )
            : this( connectionString.ConnectionString, connectionString.ProviderName, connectionString.Name )
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <param name="providerName">Db provider name</param>
        public DataAccessConnectionDb( string connectionString, string providerName, string name )
        {
            ConnectionString = connectionString;
            ProviderName = providerName;
            Name = name;

            transactions = new Stack<DataAccessTransactionDb>();

            dbProviderFactory = DbProviderFactories.GetFactory( ProviderName );
            if ( dbProviderFactory == null )
                throw new Exception( string.Format( "DbProviderFactory not found for db provider [{0}]", ProviderName ) );
        }

        /// <summary>
        /// Begin transaction - implementation interface IDataAccessConnection.
        /// May be used as: ((IDataAccessConnection)this).BeginTransaction()
        /// </summary>
        /// <returns>IDataAccessTransaction instances</returns>
        IDataAccessTransaction IDataAccessConnection.BeginTransaction()
        {
            return (IDataAccessTransaction)BeginTransaction();
        }
        /// <summary>
        /// Begin transaction
        /// </summary>
        /// <returns>DataAccessTransactionDb transaction object</returns>
        public DataAccessTransactionDb BeginTransaction()
        {
            return BeginTransaction( null );
        }
        /// <summary>
        /// Begin transaction - implementation interface IDataAccessConnection.
        /// May be used as: ((IDataAccessConnection)this).BeginTransaction( parentTransaction )
        /// </summary>
        /// <param name="parentTransaction">Parent Transaction that begin this transaction</param>
        /// <returns>IDataAccessTransaction instances</returns>
        IDataAccessTransaction IDataAccessConnection.BeginTransaction( Transaction parentTransaction )
        {
            return (IDataAccessTransaction)BeginTransaction( parentTransaction );
        }
        /// <summary>
        /// Begin transaction
        /// </summary>
        /// <param name="parentTransaction">Parent Transaction that begin this transaction</param>
        /// <returns>DataAccessTransactionDb transaction object</returns>
        public DataAccessTransactionDb BeginTransaction( Transaction parentTransaction )
        {
            CheckState();

            var transaction = new DataAccessTransactionDb( this, parentTransaction );

            transactions.Push( transaction );

            return transaction;
        }
        /// <summary>
        /// Commit transaction
        /// </summary>
        /// <param name="transaction">DataAccessTransactionDb transaction object for commit</param>
        internal void CommitTransaction( DataAccessTransactionDb transaction )
        {
            if ( transactions.Count == 0 )
                throw new TransactionException( "There is no started transactions" );

            if ( transactions.First() != transaction )
                throw new TransactionException( "This is not last started transaction" );

            

            if ( transactions.Count == 1 )
            {
                // This is last DataAccess Transaction. Commit real db transaction, if it`s exists
                if ( dbTransaction != null )
                {
                    // Уведомляем основное соединение о начале физического коммита
                    if ( transaction.ParentTransaction != null )
                        transaction.ParentTransaction.Connection.OnPhysicalCommit();

                    var tmpTransaction = dbTransaction;
                    var tmpConnection = dbTransaction.Connection;
                    dbTransaction = null;

                    try
                    {
                        tmpTransaction.Commit();
                        tmpConnection.Close();
                    }
                    catch ( Exception ex )
                    {
                        // Принудительное закрытие соединения без проброса ошибки
                        // использовать просто using для вызова Dispose нельзя так как если будет ошибка
                        // при вызове Dispose в using то наш Exception не дойдет
                        // https://msdn.microsoft.com/ru-ru/library/aa355056%28v=vs.110%29.aspx
                        try { tmpConnection.Dispose(); } catch { }

                        throw new TransactionUnknowStateException( ex.Message, ex );
                    }
                }
            }

            transactions.Pop();
        }
        /// <summary>
        /// Rollback transaction
        /// </summary>
        /// <param name="transaction">DataAccessTransactionDb transaction object for rollback</param>
        internal void RollbackTransactions( DataAccessTransactionDb transaction )
        {
            // Сохраняем первое исключение
            Exception firstException = null;

            // Rollback real db transaction
            if ( dbTransaction != null )
            {
                var tmpTransaction = dbTransaction;
                var tmpConnection = dbTransaction.Connection;
                dbTransaction = null;

                try
                {
                    if ( tmpTransaction != null )
                    {
                        tmpTransaction.Rollback();
                    }
                    if ( tmpConnection != null )
                    {
                        tmpConnection.Close();
                    }
                }
                catch ( Exception ex )
                {
                    if ( firstException == null )
                        firstException = ex;

                    // Принудительное закрытие соединения без проброса ошибки
                    // использовать просто using для вызова Dispose нельзя так как если будет ошибка
                    // при вызове Dispose в using то наш Exception не дойдет
                    // https://msdn.microsoft.com/ru-ru/library/aa355056%28v=vs.110%29.aspx
                    try { tmpConnection.Dispose(); } catch { }
                }
            }


            // Rollback all runned transactions
            if ( TransactionsCount > 0 )
            {
                var transactionsToRollback = transactions;
                transactions = new Stack<DataAccessTransactionDb>();

                foreach ( var item in transactionsToRollback )
                {
                    try
                    {
                        item.Rollback();

                        if ( item.ParentTransaction != null )
                            item.ParentTransaction.Rollback();
                    }
                    catch ( Exception ex )
                    {
                        if ( firstException == null )
                            firstException = ex;
                    }

                }
            }

            if ( firstException != null )
                throw new TransactionUnknowStateException( firstException.Message, firstException );

        }

        /// <summary>
        /// Check state
        /// </summary>
        private void CheckState()
        {
            if ( TransactionsCount == 0 && dbTransaction != null )
                throw new TransactionUnknowStateException(
                    string.Format( "Unkonow state. DataAccessConnectionDb of type [{0}] for providerName = [{1}] has 0 started transactions, but has real DbTransaction",
                        GetType().Name,
                        ProviderName ) );
        }
    }
}