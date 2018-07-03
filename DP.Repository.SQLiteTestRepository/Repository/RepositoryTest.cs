using DP.Repository.SQLiteTestRepository.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository.SQLiteTestRepository.Repository
{
    /// <summary>
    /// Тестовый репозиторий.
    /// Строится вокруг двух идентичных тестовых баз данных SQLite, управляемых разными коннекциями.
    /// Наследуюемся от базового репозитория, чтобы упростить реализацию IRepository
    /// </summary>
    public class RepositoryTest : RepositoryBase
    {
        /// <summary>
        /// DataAccessDb для первой базы данных SQLite. 
        /// Использование public только для тестового репозитория, в рабочих best practice - private.
        /// </summary>
        public DataAccessSQLiteTest DataAccessSQLiteTest1 { get; private set; }
        /// <summary>
        /// DataAccessDb для второй базы данных SQLite. 
        /// Использование public только для тестового репозитория, в рабочих best practice - private.
        /// </summary>
        public DataAccessSQLiteTest DataAccessSQLiteTest2 { get; private set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="connection">Основное соединение</param>
        /// <param name="dataAccessSQLiteTest1">DataAccessDb для первой тестовой базы</param>
        /// <param name="dataAccessSQLiteTest2">DataAccessDb для второй тестовой базы</param>
        public RepositoryTest( Connection connection, DataAccessSQLiteTest dataAccessSQLiteTest1, DataAccessSQLiteTest dataAccessSQLiteTest2 )
            : base( connection )
        {
            DataAccessSQLiteTest1 = dataAccessSQLiteTest1;
            DataAccessSQLiteTest2 = dataAccessSQLiteTest2;
        }

        /// <summary>
        /// Вставка первичных ключей в первую и вторую базу.
        /// Внутри объединяется в единую транзакцию.
        /// </summary>
        /// <param name="idTo1Db">Id для вставки в первую базу</param>
        /// <param name="idTo2Db">Id для вставки во вторую базу</param>
        public void InsertIds( int idTo1Db, int idTo2Db )
        {
            using ( var transaction = Connection.BeginTransaction() )
            {
                DataAccessSQLiteTest1.InsertId( idTo1Db );
                DataAccessSQLiteTest2.InsertId( idTo2Db );

                transaction.Commit();
            }
        }

        /// <summary>
        /// Вставка Id в первую тестовую базу
        /// </summary>
        /// <param name="id">Id</param>
        public void InsertIdInDb1( int id )
        {
            using ( var transaction = Connection.BeginTransaction() )
            {
                DataAccessSQLiteTest1.InsertId( id );
                transaction.Commit();
            }

        }

        /// <summary>
        /// Вставка Id во вторую тестовую базу
        /// </summary>
        /// <param name="id">Id</param>
        public void InsertIdInDb2( int id )
        {
            using ( var transaction = Connection.BeginTransaction() )
            {
                DataAccessSQLiteTest2.InsertId( id );
                transaction.Commit();
            }
        }

        /// <summary>
        /// Получение списка Id из первой базы
        /// </summary>
        /// <returns>Список Id</returns>
        public IList<int> GetIdsFromDb1()
        {
            return DataAccessSQLiteTest1.GetIds();
        }

        /// <summary>
        /// Получение списка Id из второй базы
        /// </summary>
        /// <returns>Список Id</returns>
        public IList<int> GetIdsFromDb2()
        {
            return DataAccessSQLiteTest2.GetIds();
        }

        /// <summary>
        /// Очистка тестовых баз данных.
        /// Вызовы не объеденены в транзакцию, что не есть best practice
        /// </summary>
        public void Clear()
        {
            DataAccessSQLiteTest1.Clear();
            DataAccessSQLiteTest2.Clear();
        }
    }
}
