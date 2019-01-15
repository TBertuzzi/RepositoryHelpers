using System;

namespace RepositoryHelpers
{
    public class PrimaryKey : Attribute
    {
        public override string ToString()
        {
            return "PrimaryKey";
        }
    }
}
