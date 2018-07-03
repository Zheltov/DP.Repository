using DP.Repository.DataAccess.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository.SQLiteTestRepository.DataAccess
{
    /// <summary>
    /// Тестовый IDataAccess для работы с SQLite базой данных содержащей одну тестовую таблицу:
    /// create table Test(id integer primary key)
    /// </summary>
    public class DataAccessSQLiteTest : DataAccessDb
    {
        public DataAccessSQLiteTest( DataAccessConnectionDb connection )
            : base( connection )
        { }

        /// <summary>
        /// Вставка id в тестовую таблицу. 
        /// </summary>
        /// <param name="id">Значение первичного ключа</param>
        public void InsertId( int id )
        {
            // Явно запускаем транзакцию, хотя она и не нужна, т.к. будет не явно запущена в вызове ExecuteSql
            using( var transaction = Connection.BeginTransaction() )
            {
                ExecuteSql( string.Format( "insert into Test(Id) values({0});", id ) );
                transaction.Commit();
            }
        }

        /// <summary>
        /// Получение списка id из тестовой таблицы
        /// </summary>
        /// <returns>Список Id</returns>
        public IList<int> GetIds()
        {
            var result = new List<int>();

            using( var dataTable = GetDataTable( "select Id from Test" ))
            {
                foreach( DataRow row in dataTable.Rows)
                    result.Add( int.Parse( row["Id"].ToString() ) );
            }

            return result;
        }

        /// <summary>
        /// Удаление записей из тестовой таблицы
        /// </summary>
        public void Clear()
        {
            ExecuteSql( "delete from Test;" );
        }
    }
}
