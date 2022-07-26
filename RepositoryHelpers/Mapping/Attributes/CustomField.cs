using System;
using System.Collections.Generic;
using System.Text;

namespace RepositoryHelpers
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomField : Attribute
    {
        public string FieldName { get; private set; }

        public CustomField(string fieldName) =>
            FieldName = fieldName;
    }
}
