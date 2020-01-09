using System.Reflection;

namespace RepositoryHelpers.Utils
{
    internal static class CustomAttributeDataExtensions
    {
        public static string GetAttributeName(this CustomAttributeData customAttribute)
            => customAttribute.AttributeType.ToString().ToUpperInvariant();
    }
}