using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace HalApplicationBuilder.Core {
    internal interface IAggregateSetting {
        internal static IAggregateSetting FromReflection(Config config, Type rootAggregateType) {
            return new ReflectionAggregate(config, rootAggregateType);
        }

        string DisplayName { get; }
        IEnumerable<AggregateMember> GetMembers(Aggregate owner);

        private class ReflectionAggregate : IAggregateSetting {
            public ReflectionAggregate(Config config, Type aggregateType) {
                _config = config;
                _aggregateType = aggregateType;
            }
            private readonly Config _config;
            private readonly Type _aggregateType;

            public string DisplayName => _aggregateType.Name;

            public IEnumerable<AggregateMember> GetMembers(Aggregate owner) {
                foreach (var prop in _aggregateType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                    if (prop.GetCustomAttribute<NotMappedAttribute>() != null) continue;

                    var displayName = prop.Name;
                    var isPrimary = prop.GetCustomAttribute<KeyAttribute>() != null;

                    if (MemberImpl.SchalarValue.IsPrimitive(prop.PropertyType)) {
                        yield return new MemberImpl.SchalarValue(_config, displayName, isPrimary, owner, prop.PropertyType);

                    } else if (prop.PropertyType.IsGenericType
                        && prop.PropertyType.GetGenericTypeDefinition() == typeof(Child<>)) {

                        var childType = prop.PropertyType.GetGenericArguments()[0];
                        var variations = prop.GetCustomAttributes<VariationAttribute>();

                        if (!childType.IsAbstract && !variations.Any()) {
                            var childSetting = FromReflection(_config, childType);
                            yield return new MemberImpl.Child(_config, displayName, isPrimary, owner, childSetting);

                        } else if (childType.IsAbstract && variations.Any()) {
                            var variationSettings = variations.ToDictionary(v => v.Key, v => FromReflection(_config, v.Type));
                            yield return new MemberImpl.Variation(_config, displayName, isPrimary, owner, variationSettings);

                        } else {
                            throw new InvalidOperationException($"抽象型ならバリエーション必須、抽象型でないなら{nameof(VariationAttribute)}指定不可");
                        }
                    } else if (prop.PropertyType.IsGenericType
                        && prop.PropertyType.GetGenericTypeDefinition() == typeof(Children<>)) {
                        var childSetting = FromReflection(_config, prop.PropertyType.GetGenericArguments()[0]);
                        yield return new MemberImpl.Children(_config, displayName, isPrimary, owner, childSetting);

                    } else if (prop.PropertyType.IsGenericType
                        && prop.PropertyType.GetGenericTypeDefinition() == typeof(RefTo<>)) {
                        var childSetting = FromReflection(_config, prop.PropertyType.GetGenericArguments()[0]);
                        yield return new MemberImpl.Reference(_config, displayName, isPrimary, owner, childSetting);

                    } else {
                        throw new InvalidOperationException($"{DisplayName} の {prop.Name} の型 {prop.PropertyType.Name} は非対応");
                    }
                }
            }
        }
    }

    //internal interface IAggregateMemberSetting {
    //    string PhysicalName { get; }
    //    string DisplayName { get; }
    //    string DbColumnName { get; }
    //    bool IsPrimary { get; }
    //    bool IsNullable { get; }
    //    Type 
    //}
}
