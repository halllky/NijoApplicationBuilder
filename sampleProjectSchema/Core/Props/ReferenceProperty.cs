using System;
using System.Collections.Generic;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class ReferenceProperty : AggregatePropBase {

        public Aggregate ReferedAggregate => Context.GetAggregate(UnderlyingPropInfo.PropertyType);

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        public override IEnumerable<PropertyTemplate> ToDbColumnModel() {
            foreach (var foreignKey in ReferedAggregate.GetDbTablePK()) {
                yield return new PropertyTemplate {
                    PropertyName = $"{UnderlyingPropInfo.Name}__{foreignKey.PropertyName}",
                    CSharpTypeName = foreignKey.CSharpTypeName,
                };
            }
        }

        public override IEnumerable<PropertyTemplate> ToListItemMember() {
            yield break;
        }
    }
}
