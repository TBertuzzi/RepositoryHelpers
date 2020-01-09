using System;

namespace RepositoryHelpers
{
    internal struct Attributes : IEquatable<Attributes>
    {
        public const string DapperIgnore = "REPOSITORYHELPERS.DAPPERIGNORE";
        public const string PrimaryKey = "REPOSITORYHELPERS.PRIMARYKEY";
        public const string IdentityIgnore = "REPOSITORYHELPERS.IDENTITYIGNORE";

        public bool Equals(Attributes other) =>
            base.Equals(other);
    }
}