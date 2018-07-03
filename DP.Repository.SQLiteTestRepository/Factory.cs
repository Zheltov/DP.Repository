using DP.Repository.DataAccess;
using DP.Repository.DataAccess.Common;
using DP.Repository.SQLiteTestRepository.DataAccess;
using DP.Repository.SQLiteTestRepository.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository.SQLiteTestRepository
{
    /// <summary>
    /// Фабрика
    /// </summary>
    public static class Factory
    {
        /// <summary>
        /// Создание тестовой конфигурации репозитория
        /// </summary>
        /// <returns>Тестовый репозиторий</returns>
        public static RepositoryTest GetRepositoryTest()
        {
            // Соединение с первой тестовой базой
            var dataAccessConnection1 = new DataAccessConnectionDb( "Data Source=DataBases\\TestDb1.db;", "System.Data.SQLite", "Test1" );
            // Соединение со второй тестовой базой
            var dataAccessConnection2 = new DataAccessConnectionDb( "Data Source=DataBases\\TestDb2.db;", "System.Data.SQLite", "Test2" );

            // Основное управляющее соединение
            Connection connection = new Connection( new List<IDataAccessConnection>() { dataAccessConnection1, dataAccessConnection2 } );

            // Создаем репозиторий и передаем в него непосредственно DataAccessDb для тестовых баз данных
            return new RepositoryTest(
                connection,
                new DataAccessSQLiteTest( dataAccessConnection1 ),
                new DataAccessSQLiteTest( dataAccessConnection2 ) );
        }
    }
}
