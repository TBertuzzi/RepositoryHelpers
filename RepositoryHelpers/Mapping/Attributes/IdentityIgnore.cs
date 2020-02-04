using System;

namespace RepositoryHelpers
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IdentityIgnore : Attribute { }
}
