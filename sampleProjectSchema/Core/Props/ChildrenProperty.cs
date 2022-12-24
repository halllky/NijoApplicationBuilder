using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;

namespace haldoc.Core.Props {
    public class ChildrenProperty : AggregatePropBase {

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield return Context.GetOrCreateAggregate(
                UnderlyingPropInfo.PropertyType.GetGenericArguments()[0],
                this,
                asChildren: true);
        }

        public override IEnumerable<PropertyTemplate> ToDbColumnModel() {
            yield break;
        }

        public override IEnumerable<PropertyTemplate> ToSearchConditionModel() {
            yield break;
        }
        public override IEnumerable<string> GenerateSearchConditionLayout(string modelPath) {
            yield break;
        }

        public override IEnumerable<PropertyTemplate> ToListItemModel() {
            yield break;
        }
    }
}
