using System;
using System.Collections.Generic;
using System.Text;

namespace RepositoryHelpers.Utils
{
    public class CustomRepositoryException : Exception
    {
        public CustomRepositoryException()
        {
        }

        public CustomRepositoryException(string message)
            : base(message)
        {
        }

        public CustomRepositoryException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
