using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository.DataAccess
{
    /// <summary>
    /// DataAccessConnection interface
    /// </summary>
    public interface IDataAccessConnection
    {
        string Name { get; }
        /// <summary>
        /// Count started transactions
        /// </summary>
        int TransactionsCount { get; }

        /// <summary>
        /// Begin new DataAccess Transaction
        /// </summary>
        /// <returns>IDataAccessTransaction</returns>
        IDataAccessTransaction BeginTransaction();

        /// <summary>
        /// Begin new DataAccess Transaction
        /// </summary>
        /// <param name="parentTransaction">Parent Transaction started in Connection</param>
        /// <returns>IDataAccessTransaction</returns>
        IDataAccessTransaction BeginTransaction( Transaction parentTransaction );
    }
}