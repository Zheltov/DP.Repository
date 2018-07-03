using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP.Repository
{
    /// <summary>
    /// Interface of repository
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// Main connection
        /// </summary>
        Connection Connection { get; }
    }
}