using System;

namespace Aurora.DataManager
{
    public class DataManagerException : Exception
    {
        public DataManagerException(string message) : base(message)
        {
        }
    }
}