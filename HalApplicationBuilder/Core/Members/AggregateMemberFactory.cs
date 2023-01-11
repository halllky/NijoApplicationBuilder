using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace HalApplicationBuilder.Core.Members {

    internal class AggregateMemberFactory : Core.IAggregateMemberFactory {
        internal AggregateMemberFactory(IServiceProvider serviceProvider) {
            _service = serviceProvider;
        }

        private readonly IServiceProvider _service;

        public IEnumerable<IAggregateMember> CreateMembers(Aggregate aggregate) {

            foreach (var prop in aggregate.UnderlyingType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (prop.GetCustomAttribute<NotMappedAttribute>() != null) continue;

                if (SchalarValue.IsPrimitive(prop.PropertyType)) {
                    yield return new SchalarValue(prop, aggregate, _service);

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Child<>)) {

                    var childType = prop.PropertyType.GetGenericArguments()[0];
                    var variations = prop.GetCustomAttributes<VariationAttribute>();

                    if (!childType.IsAbstract && !variations.Any())
                        yield return new Child(prop, aggregate, _service);

                    else if (childType.IsAbstract && variations.Any())
                        yield return new Variation(prop, aggregate, _service);

                    else
                        throw new InvalidOperationException($"抽象型ならバリエーション必須、抽象型でないなら{nameof(VariationAttribute)}指定不可");

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Children<>)) {
                    yield return new Children(prop, aggregate, _service);

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(RefTo<>)) {
                    yield return new Reference(prop, aggregate, _service);

                } else {
                    throw new InvalidOperationException($"{aggregate.UnderlyingType.Name} の {prop.Name} の型 {prop.PropertyType.Name} は非対応");
                }
            }
        }
    }
}
