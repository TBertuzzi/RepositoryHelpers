using System;
namespace RepositoryHelpers
{
    public sealed class DapperIgnore : Attribute
    {
        public override string ToString()
        {
            return "DapperIgnore";
        }
    }
}
