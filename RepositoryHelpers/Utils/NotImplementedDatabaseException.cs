using System;

namespace RepositoryHelpers.Utils
{
    public class NotImplementedDatabaseException : Exception
    {
      
        public NotImplementedDatabaseException()
            : base("Not implemented for selected database")
        {
        }

        public NotImplementedDatabaseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}