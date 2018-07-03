using System;
using System.Reflection;
using System.Data.Common;
using NUnit.Framework;
using DP.Repository.DataAccess.Common;
using DP.Repository.SQLiteTestRepository;

namespace DP.Repository.UnitTest
{
    /// <summary>
    /// Специальные тесты с моделированием ошибок в узких местах Commit & Rollback внутри DataAccessTransactionDb
    /// </summary>
    [TestFixture]
    public class SpecialExceptions
    {
        /// <summary>
        /// Эмуляцию Exception при операции commint в первой физической транзакции.
        /// С помощью reflection добираемся до dbTransaction DataAccessTransactionDb и после операций вставки у dbTransaction закрываем connection
        /// </summary>
        [Test]
        public void ExceptionOnCommitDb1()
        {
            var repository = Factory.GetRepositoryTest();
            repository.Clear();

            using ( var transaction = repository.Connection.BeginTransaction() )
            {
                repository.InsertIdInDb1( 1 );
                repository.InsertIdInDb2( 2 );

                // Получаем транзакцию через reflection и закрываем соединение
                var field = typeof( DataAccessConnectionDb ).GetField( "dbTransaction", BindingFlags.NonPublic | BindingFlags.Instance );
                var dbTransaction = ( DbTransaction )field.GetValue( repository.DataAccessSQLiteTest1.Connection );
                dbTransaction.Connection.Close();

                // Должны получить TransactionUnknowStateException, так как физическое соединение уже закрыли
                try
                {
                    transaction.Commit();
                    Assert.Fail( "Not throwed exception" );
                }
                catch ( Exception ex )
                {
                    Assert.IsTrue( ex is TransactionUnknowStateException );
                }
            }
            // Данных в базах не осталось
            Assert.AreEqual( 0, repository.GetIdsFromDb1().Count );
            Assert.AreEqual( 0, repository.GetIdsFromDb2().Count );

            // Транзакциий нет
            Assert.AreEqual( 0, repository.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest1.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest2.Connection.TransactionsCount );
        }

        /// <summary>
        /// Эмуляцию Exception при операции commint в первой физической транзакции.
        /// С помощью reflection добираемся до dbTransaction DataAccessTransactionDb и после операций вставки у dbTransaction закрываем connection
        /// </summary>
        [Test]
        public void ExceptionOnCommitDb2()
        {
            var repository = Factory.GetRepositoryTest();
            repository.Clear();

            using ( var transaction = repository.Connection.BeginTransaction() )
            {
                repository.InsertIdInDb1( 1 );
                repository.InsertIdInDb2( 2 );

                // Получаем транзакцию через reflection и закрываем соединение
                var field = typeof( DataAccessConnectionDb ).GetField( "dbTransaction", BindingFlags.NonPublic | BindingFlags.Instance );
                var dbTransaction = ( DbTransaction )field.GetValue( repository.DataAccessSQLiteTest2.Connection );
                dbTransaction.Connection.Close();

                // Должны получить TransactionUnknowStateException, так как физическое соединение уже закрыли
                try
                {
                    transaction.Commit();
                    Assert.Fail( "Not throwed exception" );
                }
                catch ( TransactionUnknowStateException ex )
                {
                    Assert.IsTrue( ex is TransactionUnknowStateException );
                }
            }
            // В первую базу мы усепли закоммитить одну запись
            Assert.AreEqual( 1, repository.GetIdsFromDb1().Count );

            // Во второй базе ни чего быть не должно
            Assert.AreEqual( 0, repository.GetIdsFromDb2().Count );

            // Транзакциий нет
            Assert.AreEqual( 0, repository.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest1.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest2.Connection.TransactionsCount );
        }

        /// <summary>
        /// Эмуляцию Exception при операции rollback во второй физической транзакции.
        /// С помощью reflection добираемся до dbTransaction DataAccessTransactionDb и после операций вставки у dbTransaction закрываем connection
        /// </summary>
        [Test]
        public void ExceptionOnRollbackDb1()
        {
            var repository = Factory.GetRepositoryTest();
            repository.Clear();

            using ( var transaction = repository.Connection.BeginTransaction() )
            {
                repository.InsertIdInDb1( 1 );
                repository.InsertIdInDb2( 2 );

                // Получаем транзакцию через reflection и закрываем соединение
                var field = typeof( DataAccessConnectionDb ).GetField( "dbTransaction", BindingFlags.NonPublic | BindingFlags.Instance );
                var dbTransaction = (DbTransaction)field.GetValue( repository.DataAccessSQLiteTest1.Connection );
                dbTransaction.Connection.Close();

                // Должны получить TransactionUnknowStateException, так как физическое соединение уже закрыли
                try
                {
                    transaction.Rollback();
                    Assert.Fail( "Not throwed exception" );
                }
                catch ( TransactionUnknowStateException ex )
                {
                    Assert.IsTrue( ex is TransactionUnknowStateException );
                }
            }
            // В базах ничего не должно быть
            Assert.AreEqual( 0, repository.GetIdsFromDb1().Count );
            Assert.AreEqual( 0, repository.GetIdsFromDb2().Count );

            // Транзакциий нет
            Assert.AreEqual( 0, repository.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest1.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest2.Connection.TransactionsCount );
        }

        /// <summary>
        /// Эмуляцию Exception при операции rollback в первой физической транзакции.
        /// С помощью reflection добираемся до dbTransaction DataAccessTransactionDb и после операций вставки у dbTransaction закрываем connection
        /// </summary>
        [Test]
        public void ExceptionOnRollbackDb2()
        {
            var repository = Factory.GetRepositoryTest();
            repository.Clear();

            using ( var transaction = repository.Connection.BeginTransaction() )
            {
                repository.InsertIdInDb1( 1 );
                repository.InsertIdInDb2( 2 );

                // Получаем транзакцию через reflection и закрываем соединение
                var field = typeof( DataAccessConnectionDb ).GetField( "dbTransaction", BindingFlags.NonPublic | BindingFlags.Instance );
                var dbTransaction = (DbTransaction)field.GetValue( repository.DataAccessSQLiteTest1.Connection );
                dbTransaction.Connection.Close();

                // Должны получить TransactionUnknowStateException, так как физическое соединение уже закрыли
                try
                {
                    transaction.Rollback();
                    Assert.Fail( "Not throwed exception" );
                }
                catch ( TransactionUnknowStateException ex )
                {
                    Assert.IsTrue( ex is TransactionUnknowStateException );
                }
            }
            // В базах ничего не должно быть
            Assert.AreEqual( 0, repository.GetIdsFromDb1().Count );
            Assert.AreEqual( 0, repository.GetIdsFromDb2().Count );

            // Транзакциий нет
            Assert.AreEqual( 0, repository.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest1.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest2.Connection.TransactionsCount );
        }
    }
}
