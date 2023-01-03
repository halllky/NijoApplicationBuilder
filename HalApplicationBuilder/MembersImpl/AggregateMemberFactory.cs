using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using HalApplicationBuilder.Core;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder.MembersImpl {

    internal class AggregateMemberFactory : Core.IAggregateMemberFactory {
        internal AggregateMemberFactory(IServiceProvider serviceProvider) {
            _service = serviceProvider;
        }

        private readonly IServiceProvider _service;

        public IEnumerable<IAggregateMember> CreateMembers(Aggregate aggregate) {
            var schema = _service.GetRequiredService<Core.ApplicationSchema>();

            foreach (var prop in aggregate.UnderlyingType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (prop.GetCustomAttribute<NotMappedAttribute>() != null) continue;

                if (SchalarValue.IsPrimitive(prop.PropertyType)) {
                    yield return new SchalarValue { Owner = aggregate, UnderlyingPropertyInfo = prop };

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Child<>)) {
                    yield return new Child { Owner = aggregate, UnderlyingPropertyInfo = prop };

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(Children<>)) {
                    yield return new Children { Owner = aggregate, UnderlyingPropertyInfo = prop };

                } else if (prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(RefTo<>)) {
                    yield return new Reference { Owner = aggregate, UnderlyingPropertyInfo = prop };

                } else {
                    throw new InvalidOperationException($"{aggregate.UnderlyingType.Name} の {prop.Name} の型 {prop.PropertyType.Name} は非対応");
                }
            }
        }
    }
}
