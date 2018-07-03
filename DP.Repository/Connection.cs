using DP.Repository.DataAccess;
using DP.Repository.Private;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository
{
    /// <summary>
    /// Connection for repositories that union all IDataAccessConnection and control it.
    /// </summary>
    public sealed class Connection
    {
        /// <summary>
        /// Stack for all started transactions in this connection
        /// </summary>
        Stack<Transaction> transactions { get; set; }

        /// <summary>
        /// List of managed IDataAccessConnection
        /// </summary>
        internal IList<IDataAccessConnection> DataAccessConnections { get; set; }

        /// <summary>
        /// Count started transactions in this connection
        /// </summary>
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
        public Connection() : this( null ) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataAccessConnections">List of IDataAccessConnection for manage</param>
        public Connection( IList<IDataAccessConnection> dataAccessConnections )
        {
            transactions = new Stack<Transaction>();
            DataAccessConnections = new List<IDataAccessConnection>();
            _expressions = new List<ExpressionMethodCall>();

            if ( dataAccessConnections != null )
                foreach ( var dataAccessConnection in dataAccessConnections )
                    AddDataAccessConnection( dataAccessConnection );
        }

        /// <summary>
        /// Add DataAccessConnection for manage
        /// </summary>
        /// <param name="item">IDataAccessConnection</param>
        public void AddDataAccessConnection( IDataAccessConnection item )
        {
            if ( DataAccessConnections.Contains( item ) )
                throw new Exception( string.Format( "Instance of [{0}] already exists in DataAccessConnections", item.GetType() ) );

            if ( DataAccessConnections.FirstOrDefault( x => x.Name == item.Name ) != null )
                throw new Exception( string.Format( "Connection with name [{0}] already exists in DataAccessConnections", item.Name ) );

            if ( TransactionsCount != 0 )
                throw new Exception( "Connection already has started transactions" );

            DataAccessConnections.Add( item );
        }

        public T GetDataAccessConnection<T>( string name ) where T : IDataAccessConnection
        {
            var result = DataAccessConnections.FirstOrDefault( x => x.Name == name );

            if ( result == null)
                throw new Exception( string.Format( "Connection with name [{0}] not found", name ) );

            return (T)result;
        }

        /// <summary>
        /// Begin new transaction
        /// </summary>
        /// <returns>Transaction object</returns>
        public Transaction BeginTransaction()
        {
            CheckState();

            var transaction = new Transaction( this );
            try
            {
                transactions.Push( transaction );

                return transaction;
            }
            catch ( Exception )
            {
                if ( transaction != null )
                    transaction.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Commit transaction
        /// </summary>
        /// <param name="transaction">Transaction for commit</param>
        internal void CommitTransaction( Transaction transaction )
        {
            if ( transactions.Count == 0 )
                throw new TransactionException( "There is no started transactions" );

            if ( transactions.First() != transaction )
                throw new TransactionException( "This is not last started transaction" );

            transactions.Pop();
        }

        /// <summary>
        /// Rollback transaction
        /// </summary>
        /// <param name="transaction">Transaction for rollback</param>
        internal void RollbackTransactions( Transaction transaction )
        {
            if ( transactions.Count == 0 )
                return;

            var transactionsToRollback = transactions;
            transactions = new Stack<Transaction>();

            _expressions.Clear();

            foreach ( var item in transactionsToRollback )
                if ( item != transaction )
                    item.Rollback();
        }

        /// <summary>
        /// Добавить триггер
        /// </summary>
        /// <param name="expression"></param>
        internal void AddTrigger( Expression<Action> expression )
        {
            var result = new ExpressionMethodCall( expression );
            var find = _expressions.FirstOrDefault( x => x.Equals( result ) );
            if ( find == null )
                _expressions.Add( result );
        }

        /// <summary>
        /// Вызывается перед выполнением физического коммита
        /// </summary>
        internal void OnPhysicalCommit()
        {
            ExecuteTriggers();
        }

        /// <summary>
        /// Выполнение триггеров
        /// </summary>
        void ExecuteTriggers()
        {
            foreach ( var expression in _expressions )
            {
                var action = expression.Expression.Compile();
                action.Invoke();
            }
            _expressions.Clear();
        }

        /// <summary>
        /// Check state of this connection
        /// </summary>
        void CheckState()
        {
            // Не должно быть запущено меньше транзакций чем тут
            foreach ( var dataAccessTransactionManager in DataAccessConnections )
                if ( dataAccessTransactionManager.TransactionsCount < TransactionsCount )
                {
                    throw new TransactionUnknowStateException(
                        string.Format( "Unkonow state. Connection has [{0}] started transactions, but DataAccessTransactionManager of type [{1}] has [{2}] started transactions",
                            TransactionsCount,
                            dataAccessTransactionManager.GetType().Name,
                            dataAccessTransactionManager.TransactionsCount ) );
                }
        }

        /// <summary>
        /// Список выражений, которые будут выполнены при первом физическом коммите
        /// </summary>
        readonly IList<ExpressionMethodCall> _expressions;
    }

    /// <summary>
    /// Exception throwed then exists problems with transactions
    /// </summary>
    public class TransactionException : Exception
    {
        public TransactionException( string message ) : base( message ) { }
    }

    /// <summary>
    /// Exception throwed then connection or IDataAccessConnection proceeds in unknown transactions state.
    /// Also can be innerException
    /// </summary>
    public class TransactionUnknowStateException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        public TransactionUnknowStateException( string message ) : base( message ) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner Exception object</param>
        public TransactionUnknowStateException( string message, Exception innerException ) : base( message, innerException ) { }
    }
}