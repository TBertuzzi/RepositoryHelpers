using System;

namespace RepositoryHelpers
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class Table : Attribute
    {
        public string TableName { get; private set; }

        public Table(string tableName) =>
            TableName = tableName;
    }
}