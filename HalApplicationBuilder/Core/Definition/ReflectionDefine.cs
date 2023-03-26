using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace HalApplicationBuilder.Core.Definition {
    internal class ReflectionDefine : IAggregateDefine {
        internal ReflectionDefine(Config config, Type aggregateType, IEnumerable<Type> rootAggregateTypes) {
            _config = config;
            _aggregateType = aggregateType;
            _rootAggregateTypes = rootAggregateTypes;
        }
        private readonly Config _config;
        private readonly Type _aggregateType;
        private readonly IEnumerable<Type> _rootAggregateTypes;

        public string DisplayName => _aggregateType.Name;

        private Aggregate GetAggregateByRefTargetType(Type refTargetType) {
            var matches = _rootAggregateTypes
                .Select(root => new RootAggregate(_config, new ReflectionDefine(_config, root, _rootAggregateTypes)))
                .SelectMany(rootAggregate => rootAggregate.GetDescendantsAndSelf())
                .Where(aggregate => ((ReflectionDefine)aggregate.Def)._aggregateType == refTargetType)
                .ToArray();
            if (matches.Length == 0)
                throw new InvalidOperationException($"'{refTargetType.Name}' と対応する集約が見つかりません。");
            if (matches.Length >= 2)
                throw new InvalidOperationException($"'{refTargetType.Name}' と対応する集約が複数見つかりました。");

            return matches.Single();
        }

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
                        var childSetting = new ReflectionDefine(_config, childType, _rootAggregateTypes);
                        yield return new MemberImpl.Child(_config, displayName, isPrimary, owner, childSetting);

                    } else if (childType.IsAbstract && variations.Any()) {
                        var variationSettings = variations.ToDictionary(v => v.Key, v => (IAggregateDefine)new ReflectionDefine(_config, v.Type, _rootAggregateTypes));
                        yield return new MemberImpl.Variation(_config, displayName, isPrimary, owner, variationSettings);

                    } else {
                        throw new InvalidOperationException($"抽象型ならバリエーション必須、抽象型でないなら{nameof(VariationAttribute)}指定不可");
                    }
                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Children<>)) {
                    var childSetting = new ReflectionDefine(_config, prop.PropertyType.GetGenericArguments()[0], _rootAggregateTypes);
                    yield return new MemberImpl.Children(_config, displayName, isPrimary, owner, childSetting);

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(RefTo<>)) {
                    var nullable = !isPrimary && prop.GetCustomAttribute<RequiredAttribute>() == null;
                    var getRefTarget = () => GetAggregateByRefTargetType(prop.PropertyType.GetGenericArguments()[0]);
                    yield return new MemberImpl.Reference(_config, displayName, isPrimary, nullable, owner, getRefTarget);

                } else {
                    throw new InvalidOperationException($"{DisplayName} の {prop.Name} の型 {prop.PropertyType.Name} は非対応");
                }
            }
        }
    }
}

