using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.Runtime;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.Core.Members {
    internal class Child : AggregateMemberBase {
        internal Child(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider)
            : base(propertyInfo, owner, serviceProvider) { }

        public override bool IsCollection => false;

        private Aggregate _child;
        internal Aggregate ChildAggregate {
            get {
                if (_child == null) {
                    var childType = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                    _child = new Aggregate(childType, this, MemberFactory);
                }
                return _child;
            }
        }

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield return ChildAggregate;
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            // ナビゲーションプロパティ
            yield return new DbColumn {
                Virtual = true,
                CSharpTypeName = DbSchema.GetDbEntity(ChildAggregate).RuntimeFullName,
                PropertyName = NavigationPropName,
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = ViewModelProvider.GetSearchConditionModel(ChildAggregate).RuntimeFullName,
                PropertyName = SearchConditionPropName,
                Initializer = "new()",
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels() {
            foreach (var childProp in ViewModelProvider.GetSearchResultModel(ChildAggregate).Properties) {
                yield return new MvcModelProperty {
                    PropertyName = SearchResultPropName(childProp),
                    CSharpTypeName = childProp.CSharpTypeName,
                    Initializer = childProp.Initializer,
                };
            }
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = ViewModelProvider.GetInstanceModel(ChildAggregate).RuntimeFullName,
                PropertyName = InstanceModelPropName,
                Initializer = "new()",
            };
        }

        internal string NavigationPropName => Name;
        internal string SearchConditionPropName => Name;
        internal string SearchResultPropName(MvcModelProperty childProp) => childProp.PropertyName; // TODO 親子でプロパティ名が重複する場合を考慮する
        internal string InstanceModelPropName => Name;

        public override IEnumerable<string> GetInvalidErrors() {
            if (IsPrimaryKey) yield return $"{Name} は子要素のため主キーに設定できません。";
        }

        private protected override void Accept(IMemberVisitor visitor) {
            visitor.Visit(this);
        }
    }
}
