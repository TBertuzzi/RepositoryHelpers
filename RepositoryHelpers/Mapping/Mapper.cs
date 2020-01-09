using System;
using Dapper.FluentMap;
using Dapper.FluentMap.Configuration;

namespace RepositoryHelpers.Mapping
{
    public class Mapper
    {
        public static void Initialize(Action<FluentMapConfiguration> configure) =>
            FluentMapper.Initialize(configure);

        public static bool IsEmptyMapping() =>
            FluentMapper.EntityMaps.IsEmpty;
    }
}