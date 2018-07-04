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
}
