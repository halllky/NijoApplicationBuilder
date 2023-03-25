using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace HalApplicationBuilder.Core.Definition {
    internal class ReflectionDefine : IAggregateDefine {
        internal ReflectionDefine(Config config, Type aggregateType) {
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
                        var childSetting = new ReflectionDefine(_config, childType);
                        yield return new MemberImpl.Child(_config, displayName, isPrimary, owner, childSetting);

                    } else if (childType.IsAbstract && variations.Any()) {
                        var variationSettings = variations.ToDictionary(v => v.Key, v => (IAggregateDefine)new ReflectionDefine(_config, v.Type));
                        yield return new MemberImpl.Variation(_config, displayName, isPrimary, owner, variationSettings);

                    } else {
                        throw new InvalidOperationException($"抽象型ならバリエーション必須、抽象型でないなら{nameof(VariationAttribute)}指定不可");
                    }
                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Children<>)) {
                    var childSetting = new ReflectionDefine(_config, prop.PropertyType.GetGenericArguments()[0]);
                    yield return new MemberImpl.Children(_config, displayName, isPrimary, owner, childSetting);

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(RefTo<>)) {
                    yield return new MemberImpl.Reference(
                        _config,
                        displayName,
                        isPrimary,
                        owner,
                        () => {
                            var def = new ReflectionDefine(_config, prop.PropertyType.GetGenericArguments()[0]);
                            return new RootAggregate(_config, def); // TODO: 子要素への参照が考慮されていない
                        });

                } else {
                    throw new InvalidOperationException($"{DisplayName} の {prop.Name} の型 {prop.PropertyType.Name} は非対応");
                }
            }
        }
    }
}

