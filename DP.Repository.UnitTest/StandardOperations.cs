using System;
using System.Data.SQLite;
using NUnit.Framework;
using DP.Repository.SQLiteTestRepository;

namespace DP.Repository.UnitTest
{
    /// <summary>
    /// Проверка некоторых стандартных ситуаций поведения
    /// </summary>
    [TestFixture]
    public class StandardOperations
    {
        /// <summary>
        /// Проверяем что происходит нормально вставка в 2 базы и последующий откат транзакции
        /// с проверкой, что ничего не вставлено и транзакции все закрыты
        /// </summary>
        [Test]
        public void SimpleRollback()
        {
            var repository = Factory.GetRepositoryTest();
            repository.Clear();

            using ( var transaction = repository.Connection.BeginTransaction() )
            {
                repository.InsertIds( 1, 2 );

                var idsDb1 = repository.GetIdsFromDb1();
                var idsDb2 = repository.GetIdsFromDb2();

                // В каждой базе по одной записи
                Assert.AreEqual( 1, idsDb1.Count );
                Assert.AreEqual( 1, idsDb2.Count );

                // В базу 1 вставили 1, в базу 2 - 2
                Assert.AreEqual( 1, idsDb1[0] );
                Assert.AreEqual( 2, idsDb2[0] );

                // Явный rollback, но можно и не делать
                transaction.Rollback();
            }
            // После Rollback данных в базах не осталось
            Assert.AreEqual( 0, repository.GetIdsFromDb1().Count );
            Assert.AreEqual( 0, repository.GetIdsFromDb2().Count );

            // Транзакциий нет
            Assert.AreEqual( 0, repository.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest1.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest2.Connection.TransactionsCount );
        }

        /// <summary>
        /// Вставка в 2 базы и последующий коммит, с проверкой, что все вставлено и транзакции все закрыты
        /// </summary>
        [Test]
        public void SimpleCommit()
        {
            var repository = Factory.GetRepositoryTest();
            repository.Clear();

            using ( var transaction = repository.Connection.BeginTransaction() )
            {
                repository.InsertIds( 1, 2 );

                using ( var transaction2 = repository.Connection.BeginTransaction() )
                {
                    repository.InsertIdInDb1( -1 );
                    transaction2.Commit();
                }

                transaction.Commit();
            }

            var idsDb1 = repository.GetIdsFromDb1();
            var idsDb2 = repository.GetIdsFromDb2();


            // В каждой базе по одной записи
            Assert.AreEqual( 2, idsDb1.Count );
            Assert.AreEqual( 1, idsDb2.Count );

            // В базу 1 вставили 1, в базу 2 - 2
            Assert.AreEqual( -1, idsDb1[0] );
            Assert.AreEqual( 1, idsDb1[1] );
            Assert.AreEqual( 2, idsDb2[0] );


            // Транзакциий нет
            Assert.AreEqual( 0, repository.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest1.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest2.Connection.TransactionsCount );
        }

        /// <summary>
        /// Вставка в 2 базы в двух транзакциях с принудительным откатом внутренней и проверкой что внешний коммит вызовет
        /// Exception и при этом в базе реально ни чего не будет и все коннекции нормально закрыты
        /// </summary>
        [Test]
        public void SimpleCommitAndRollback()
        {
            var repository = Factory.GetRepositoryTest();
            repository.Clear();

            using ( var transaction = repository.Connection.BeginTransaction() )
            {
                repository.InsertIds( 1, 2 );

                using ( var transaction2 = repository.Connection.BeginTransaction() )
                {
                    repository.InsertIdInDb1( -1 );

                    transaction2.Rollback();
                }

                try
                {
                    transaction.Commit();
                    Assert.Fail( "Not throwed exception" );
                }
                catch ( Exception ex )
                {
                    Assert.IsTrue( ex is TransactionException );
                }
            }

            // После Rollback данных в базах не осталось
            Assert.AreEqual( 0, repository.GetIdsFromDb1().Count );
            Assert.AreEqual( 0, repository.GetIdsFromDb2().Count );

            // Транзакциий нет
            Assert.AreEqual( 0, repository.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest1.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest2.Connection.TransactionsCount );
        }

        /// <summary>
        /// Вставка в 2 базы, после чего вставка во вторую и получение Exception при нарушении первичного ключа.
        /// При этом проверяется откат уже подтвержденной транзакции
        /// </summary>
        [Test]
        public void InsertInto2DbWithExceptionInSecondOperation()
        {
            var repository = Factory.GetRepositoryTest();
            repository.Clear();

            try
            {
                using ( var transaction = repository.Connection.BeginTransaction() )
                {
                    repository.InsertIds( 1, 2 );

                    using ( var transaction2 = repository.Connection.BeginTransaction() )
                    {
                        using ( var transaction3 = repository.Connection.BeginTransaction() )
                        {
                            repository.InsertIdInDb2( 3 );
                            transaction3.Commit();
                        }

                        // Проверяем значения в базах
                        var idsDb1 = repository.GetIdsFromDb1();
                        var idsDb2 = repository.GetIdsFromDb2();

                        // В каждой базе по одной записи
                        Assert.AreEqual( 1, idsDb1.Count );
                        Assert.AreEqual( 2, idsDb2.Count );

                        // В базу 1 вставили 1, в базу 2 - 2
                        Assert.AreEqual( 1, idsDb1[0] );
                        Assert.AreEqual( 2, idsDb2[0] );
                        Assert.AreEqual( 3, idsDb2[1] );

                        // Точка генерации exception. Пытаемся повторно вставить 3 во вторую базу
                        repository.InsertIdInDb2( 3 );
                        transaction2.Commit();
                    }

                    transaction.Commit();
                }

                Assert.Fail( "Not throwed exception" );
            }
            catch ( Exception ex )
            {
                if ( ex is SQLiteException )
                    Assert.IsTrue( ex is SQLiteException );
                else
                    throw;

            }

            // После Rollback данных в базах не осталось
            Assert.AreEqual( 0, repository.GetIdsFromDb1().Count );
            Assert.AreEqual( 0, repository.GetIdsFromDb2().Count );

            // Транзакциий нет
            Assert.AreEqual( 0, repository.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest1.Connection.TransactionsCount );
            Assert.AreEqual( 0, repository.DataAccessSQLiteTest2.Connection.TransactionsCount );
        }
    }
}