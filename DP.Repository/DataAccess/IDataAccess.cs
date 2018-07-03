namespace DP.Repository.DataAccess
{
    /// <summary>
    /// DataAccess interface
    /// </summary>
    public interface IDataAccess
    {
        /// <summary>
        /// DataAccess connection
        /// </summary>
        IDataAccessConnection Connection { get; }
    }
}