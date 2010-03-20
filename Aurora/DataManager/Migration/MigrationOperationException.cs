using System;

namespace Aurora.DataManager
{
    public class MigrationOperationException : Exception
    {
        public MigrationOperationException(string message):base(message)
        {
        }
    }
}