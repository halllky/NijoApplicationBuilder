using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;
using haldoc.SqlQuery;

namespace haldoc.Core.Props {
    public class ChildProperty : AggregatePropBase {

        public Aggregate ChildAggregate => Context.GetOrCreateAggregate(
                UnderlyingPropInfo.PropertyType.GetGenericArguments()[0],
                Owner);

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield return ChildAggregate;
        }

        public override IEnumerable<PropertyTemplate> ToDbColumnModel() {
            return ChildAggregate.GetDbTablePK();
        }

        public override IEnumerable<PropertyTemplate> ToListItemMember() {
            yield break;
        }
    }
}
