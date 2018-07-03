using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository
{
    /// <summary>
    /// Base repository for inheritance
    /// </summary>
    public abstract class RepositoryBase : IRepository
    {
        public Connection Connection { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">Connection object</param>
        public RepositoryBase( Connection connection )
        {
            Connection = connection;
        }
    }
}
