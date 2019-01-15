using System;
using System.Collections.Generic;
using System.Text;

namespace RepositoryHelpers
{
    public class IdentityIgnore : Attribute
    {
        public override string ToString()
        {
            return "IdentityIgnore";
        }
    }
}
