using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapper.FluentMap;
using Dapper.FluentMap.Dommel.Mapping;
using Dapper.FluentMap.Mapping;
using RepositoryHelpers.Utils;

namespace RepositoryHelpers.Mapping
{
    internal static class MappingHelper
    {
        public static List<string> GetPrimaryKey(Type type)
        {
            var properties = new List<string>();
            foreach (var property in type.GetProperties())
            {
                var isKey = property.CustomAttributes.ToList().Any(x => x.GetAttributeName() == Attributes.PrimaryKey);

                if (!isKey)
                {
                    var propertyMap = GetFluentPropertyMap(type, property);
                    if (propertyMap != null)
                        isKey = propertyMap.Key;
                }

                if (isKey)
                    properties.Add(property.Name);
            }
            return properties;
        }

        public static string GetIdentityColumn(Type type)
        {
            foreach (var property in type.GetProperties())
            {
                
                var isIdentity = property.CustomAttributes.ToList().Any(x => x.GetAttributeName() == Attributes.Identity);

                // if is not attribute identity, search Fluent
                if (!isIdentity)
                {
                    var propertyMap = GetFluentPropertyMap(type, property);
                    if (propertyMap != null)
                        isIdentity = propertyMap.Identity;
                }

                if (isIdentity)
                    return property.Name;
            }
            return null;
        }

        public static string GetTableName(Type type,DataBaseType dataBaseType)
        {
            var table = type.GetCustomAttributes(typeof(Table), true).FirstOrDefault() as Table;
            var tableNameFluent = GetFluentEntityMap(type)?.TableName;
            var tableName = string.Empty;

            if (table != null)
                tableName = table.TableName;
            else if (!string.IsNullOrWhiteSpace(tableNameFluent))
                tableName = tableNameFluent;
            else
                tableName = type.Name;

            switch (dataBaseType)
            {
                case DataBaseType.SqlServer:
                    return $"[{tableName}]";
                case DataBaseType.PostgreSQL:
                    return tableName.ToLower();
                case DataBaseType.Oracle:
                default:
                    return tableName;
            }
        }

        public static string GetFieldName(Type type, DataBaseType dataBaseType)
        {
            //var table = type.GetCustomAttributes(typeof(CustomField), true).FirstOrDefault() as Table;
            //var tableNameFluent = GetFluentEntityMap(type)?.;
            //var tableName = string.Empty;

            //if (table != null)
            //    tableName = table.TableName;
            //else if (!string.IsNullOrWhiteSpace(tableNameFluent))
            //    tableName = tableNameFluent;
            //else
            //    tableName = type.Name;

            //switch (dataBaseType)
            //{
            //    case DataBaseType.SqlServer:
            //        return $"[{tableName}]";
            //    case DataBaseType.PostgreSQL:
            //        return tableName.ToLower();
            //    case DataBaseType.Oracle:
            //    default:
            //        return tableName;
            //}

            return null;
        }

        public static bool IsIgnored(Type entityType, PropertyInfo property)
        {
            var customAttributeData = property.CustomAttributes.ToList();
            if (customAttributeData.Any())
            {
                if (customAttributeData.Any(x => x.GetAttributeName() == Attributes.DapperIgnore ||
                                                 x.GetAttributeName() == Attributes.Identity))
                    return true;
            }
            else
            {
                var propertyMap = GetFluentPropertyMap(entityType, property);
                if (propertyMap != null && (propertyMap.Ignored || propertyMap.Identity))
                    return true;
            }
            return false;
        }

        private static IDommelEntityMap GetFluentEntityMap(Type entityType) =>
            (IDommelEntityMap)FluentMapper.EntityMaps.FirstOrDefault(map => map.Key == entityType).Value;

        private static DommelPropertyMap GetFluentPropertyMap(Type entityType, PropertyInfo property)
        {
            var entityMap = GetFluentEntityMap(entityType);
            if (entityMap != null)
            {
                var propertyMap = entityMap.PropertyMaps.FirstOrDefault(p => p.PropertyInfo.Name.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));
                if (propertyMap != null)
                {
                    if (propertyMap is DommelPropertyMap)
                        return (DommelPropertyMap)propertyMap;
                    else
                        throw new CustomRepositoryException($"{entityType.Name} class mapping must inherit from DommelEntityMap base class.");
                }
            }
            return null;
        }
    }
}