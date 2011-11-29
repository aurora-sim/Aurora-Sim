namespace OpenSim.Services.Interfaces
{
    public interface IHeloServiceConnector
    {
        /// <summary>
        ///   Ask another server what it is
        /// </summary>
        /// <param name = "serverURI"></param>
        /// <returns></returns>
        string Helo(string serverURI);
    }
}