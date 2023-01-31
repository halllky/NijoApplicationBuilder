using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.AspNetMvc;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.Runtime;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.Core.Members {
    internal class Children : AggregateMemberBase {
        internal Children(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider)
            : base(propertyInfo, owner, serviceProvider) { }

        public override bool IsCollection => true;

        private Aggregate _child;
        internal Aggregate ChildAggregate {
            get {
                if (_child == null) {
                    var type = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                    _child = new Aggregate(type, this, MemberFactory);
                }
                return _child;
            }
        }

        public override IEnumerable<Core.Aggregate> GetChildAggregates() {
            yield return ChildAggregate;
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            // ナビゲーションプロパティ
            var item = DbSchema.GetDbEntity(ChildAggregate).RuntimeFullName;
            yield return new DbColumn {
                Virtual = true,
                CSharpTypeName = $"ICollection<{item}>",
                PropertyName = NavigationPropName,
                Initializer = $"new HashSet<{item}>()",
            };
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels() {
            var item = ViewModelProvider.GetInstanceModel(ChildAggregate).RuntimeFullName;
            yield return new MvcModelProperty {
                CSharpTypeName = $"List<{item}>",
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels() {
            yield break;
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels() {
            yield break;
        }

        internal string NavigationPropName => Name;
        internal string InstanceModelPropName => Name;

        public override IEnumerable<string> GetInvalidErrors() {
            if (IsPrimaryKey) yield return $"{Name} は子要素のため主キーに設定できません。";
        }

        private protected override void Accept(IMemberVisitor visitor) {
            visitor.Visit(this);
        }
    }
}
