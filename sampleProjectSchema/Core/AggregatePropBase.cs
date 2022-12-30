using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Core.Props;
using haldoc.Schema;

namespace haldoc.Core {
    public abstract class AggregatePropBase {

        public ProjectContext Context { get; init; }
        public Aggregate Owner { get; init; }
        public PropertyInfo UnderlyingPropInfo { get; init; }

        public string Name => UnderlyingPropInfo.Name;
        public bool IsPrimaryKey => UnderlyingPropInfo.GetCustomAttribute<KeyAttribute>() != null;
        public abstract bool IsListProperty { get; }

        public abstract IEnumerable<Aggregate> GetChildAggregates();

        public abstract IEnumerable<PropertyTemplate> ToDbEntityProperty(EntityFramework.DbSchema dbSchema);

        public abstract IEnumerable<PropertyTemplate> ToSearchConditionDtoProperty();
        public abstract IEnumerable<string> GenerateSearchConditionLayout(string modelPath);

        public abstract IEnumerable<PropertyTemplate> ToInstanceDtoProperty();
        public abstract string RenderSingleView(AggregateInstanceBuildContext renderingContext);

        public abstract IEnumerable<PropertyTemplate> ToListItemModel();

        public abstract IEnumerable<object> AssignMvcToDb(object mvcModel, object dbEntity);

        /// <summary>集約ルートのプロパティなら0、そこから子集約になるごとに+1</summary>
        public int GetDepth() {
            var count = 0;
            var ancestor = Owner.Parent;
            while (ancestor != null) {
                count++;
                ancestor = ancestor.Owner.Parent;
            }
            return count;
        }

        public object CreateInstanceDefaultValue() {
            throw new NotImplementedException();
        }
    }
}
