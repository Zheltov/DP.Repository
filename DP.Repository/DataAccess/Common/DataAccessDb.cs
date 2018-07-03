using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace DP.Repository.DataAccess.Common
{
    public class DataAccessDbStandard : DataAccessDb
    {
        public DataAccessDbStandard( DataAccessConnectionDb connection ) : base( connection ) {}

        public new DataTable GetDataTable( string sql, IList<DbParameter> parameters = null, bool isStoredProcedure = false )
        {
            return base.GetDataTable( sql, parameters, isStoredProcedure );
        }

        public new IList<DataTable> GetDataTables(string sql, IList<DbParameter> parameters = null, bool isStoredProcedure = false)
        {
            return base.GetDataTables( sql, parameters, isStoredProcedure );
        }

        public new int ExecuteSql(string sql, IList<DbParameter> parameters = null, bool isStoredProcedure = false)
        {
            return base.ExecuteSql( sql, parameters, isStoredProcedure );
        }

        public new object ExecuteScalar(string sql, IList<DbParameter> parameters = null, bool isStoredProcedure = false)
        {
            return base.ExecuteScalar( sql, parameters, isStoredProcedure );
        }

        public new void ExecuteDataReader(string sql, Action<DbDataReader, int> dataReaderAction, IList<DbParameter> parameters = null, bool isStoredProcedure = false)
        {
            base.ExecuteDataReader( sql, dataReaderAction, parameters, isStoredProcedure );
        }

        public new DbParameter CreateParameter( string name, object value, DbType? dbType = null )
        {
            return base.CreateParameter( name, value, dbType );
        }
    }

    /// <summary>
    /// Base class for IDataAccess implementations in Db providers infrastructure
    /// </summary>
    public abstract class DataAccessDb : IDataAccess
    {
        protected DbProviderFactory dbProviderFactory;
        
        /// <summary>
        /// Connection
        /// </summary>
        public DataAccessConnectionDb Connection { get; private set; }
        /// <summary>
        /// Implementation IDataAccess.Connection
        /// </summary>
        IDataAccessConnection IDataAccess.Connection
        {
            get { return Connection; }
        }

        /// <summary>
        /// Construcor
        /// </summary>
        /// <param name="connection">DataAccessConnectionDb</param>
        public DataAccessDb( DataAccessConnectionDb connection )
        {
            Connection = connection;
            dbProviderFactory = DbProviderFactories.GetFactory( Connection.ProviderName );
        }

        /// <summary>
        /// Get data table object by sql query
        /// </summary>
        /// <param name="sql">Sql query</param>
        /// <param name="parameters">List of Parameters. Can used specific parameters also as SqlParameter, etc</param>
        /// <param name="isStoredProcedure">True if sql is stored procedure</param>
        /// <returns>DataTable object</returns>
        protected virtual DataTable GetDataTable( string sql, IList<DbParameter> parameters = null, bool isStoredProcedure = false )
        {
            return GetDataTables( sql, parameters, isStoredProcedure )[0];
        }
        /// <summary>
        /// Get list of data tables by sql query. List created by result sets in sql.
        /// </summary>
        /// <param name="sql">Sql query</param>
        /// <param name="parameters">List of Parameters. Can used specific parameters also as SqlParameter, etc</param>
        /// <param name="isStoredProcedure">True if sql is stored procedure</param>
        /// <returns>List DataTable object</returns>
        protected virtual IList<DataTable> GetDataTables( string sql, IList<DbParameter> parameters = null, bool isStoredProcedure = false )
        {
            using ( var transaction = Connection.BeginTransaction() )
            using ( var command = CreateCommand( transaction, sql, parameters, isStoredProcedure ) )
            using ( var da = dbProviderFactory.CreateDataAdapter() )
            using ( var ds = new DataSet() )
            {
                da.SelectCommand = command;
                da.Fill( ds );

                transaction.Commit();

                var result = new List<DataTable>();
                foreach ( DataTable table in ds.Tables )
                    result.Add( table );

                return result;
            }
        }

        /// <summary>
        /// Execute sql
        /// </summary>
        /// <param name="sql">Sql query</param>
        /// <param name="parameters">List of Parameters. Can used specific parameters also as SqlParameter, etc</param>
        /// <param name="isStoredProcedure">True if sql is stored procedure</param>
        /// <returns>Result of DbCommand.ExecuteNonQuery()</returns>
        protected virtual int ExecuteSql( string sql, IList<DbParameter> parameters = null, bool isStoredProcedure = false )
        {
            using ( var transaction = Connection.BeginTransaction() )
            using ( var command = CreateCommand( transaction, sql, parameters, isStoredProcedure ) )
            {
                var result = command.ExecuteNonQuery();

                transaction.Commit();
                return result;
            }
        }
        /// <summary>
        /// Execute scalar
        /// </summary>
        /// <param name="sql">Sql query</param>
        /// <param name="parameters">List of Parameters. Can used specific parameters also as SqlParameter, etc</param>
        /// <param name="isStoredProcedure">True if sql is stored procedure</param>
        /// <returns>Result of DbCommand.ExecuteScalar()</returns>
        protected virtual object ExecuteScalar( string sql, IList<DbParameter> parameters = null, bool isStoredProcedure = false )
        {
            using ( var transaction = Connection.BeginTransaction() )
            using ( var command = CreateCommand( transaction, sql, parameters, isStoredProcedure ) )
            {

                var result = command.ExecuteScalar();

                transaction.Commit();
                return result;
            }
        }
        /// <summary>
        /// Execute data reader.
        /// </summary>
        /// <param name="sql">Sql query</param>
        /// <param name="dataReaderAction">Action with parameters: DbDataReader - current DbDataReader object, int - index of result set</param>
        /// <param name="parameters">List of Parameters. Can used specific parameters also as SqlParameter, etc</param>
        /// <param name="isStoredProcedure">True if sql is stored procedure</param>
        protected virtual void ExecuteDataReader( string sql, Action<DbDataReader, int> dataReaderAction, IList<DbParameter> parameters = null, bool isStoredProcedure = false )
        {
            using ( var transaction = Connection.BeginTransaction() )
            using ( var command = CreateCommand( transaction, sql, parameters, isStoredProcedure ) )
            {
                // Не используется using так как закрытие физической транзакции может произойти именно при этом
                // вызове transaction.Commit() но при этом еще не будет закрыт reader и можно получить Exception
                DbDataReader reader = command.ExecuteReader();
                try
                {
                    int resultIndex = 0;
                    do
                    {
                        while ( reader.Read() )
                        {
                            dataReaderAction( reader, resultIndex );
                        }
                        resultIndex++;
                    }
                    while ( reader.NextResult() );

                    reader.Close();
                    transaction.Commit();
                }
                catch ( Exception )
                {
                    reader.Close();
                    throw;
                }

            }
        }

        protected DbParameter CreateParameter( string name, object value, DbType? dbType = null )
        {
            var p = dbProviderFactory.CreateParameter();

            p.ParameterName = name;
            if ( value == null )
                p.Value = DBNull.Value;
            else
                p.Value = value;
            if ( dbType.HasValue )
                p.DbType = dbType.Value;

            //if ( value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof( Nullable<> ) && value == null )
            //    p.Value = DBNull.Value;


            return p;
        }

        /// <summary>
        /// Create command object
        /// </summary>
        /// <param name="transaction">Transaction object</param>
        /// <param name="sql">Sql query</param>
        /// <param name="parameters">List of Parameters. Can used specific parameters also as SqlParameter, etc</param>
        /// <param name="isStoredProcedure">True if sql is stored procedure</param>
        /// <returns>DbCommand object</returns>
        private DbCommand CreateCommand( DataAccessTransactionDb transaction, string sql, IList<DbParameter> parameters, bool isStoredProcedure )
        {
            DbCommand dbCommand = dbProviderFactory.CreateCommand();
            dbCommand.CommandText = sql;
            dbCommand.Connection = transaction.DbTransaction.Connection;
            dbCommand.Transaction = transaction.DbTransaction;


            if ( isStoredProcedure )
                dbCommand.CommandType = CommandType.StoredProcedure;

            if ( parameters != null )
                foreach ( var parameter in parameters )
                    dbCommand.Parameters.Add( parameter );

            return dbCommand;
        }
    }
}
