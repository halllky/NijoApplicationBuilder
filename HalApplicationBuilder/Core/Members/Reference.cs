using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.UIModel;

namespace HalApplicationBuilder.Core.Members {
    internal class Reference : AggregateMemberBase {
        internal Reference(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider)
            : base(propertyInfo, owner, serviceProvider) { }

        public override bool IsCollection => false;

        private Aggregate _refTarget;
        internal Aggregate RefTarget {
            get {
                if (_refTarget == null) {
                    var type = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0];
                    var targetAttributeId = UnderlyingPropertyInfo.GetCustomAttribute<RefTargetIdAttribute>();
                    _refTarget = AppSchema.FindByTypeOrAggregateId(type, targetAttributeId);
                    if (_refTarget == null) throw new InvalidOperationException($"{UnderlyingPropertyInfo.Name} の型 {type.FullName} の集約が定義されていません。");
                }
                return _refTarget;
            }
        }

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        private IReadOnlyList<DbColumn> _refPKs;
        internal IReadOnlyList<DbColumn> RefPKs {
            get {
                if (_refPKs == null) {
                    _refPKs = DbSchema
                        .GetDbEntity(RefTarget)
                        .PKColumns
                        .Select(foreignKey => new DbColumn {
                            CSharpTypeName = foreignKey.CSharpTypeName,
                            PropertyName = $"{Name}_{foreignKey.PropertyName}",
                        })
                        .ToList();
                }
                return _refPKs;
            }
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            var refTargetDbEntity = DbSchema.GetDbEntity(RefTarget);
            // 参照先DBの主キー
            foreach (var foreignKey in RefPKs) {
                yield return foreignKey;
            }
            // ナビゲーションプロパティ
            yield return new DbColumn {
                Virtual = true,
                CSharpTypeName = refTargetDbEntity.RuntimeFullName,
                PropertyName = Name,
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = typeof(ReferenceDTO).FullName,
                PropertyName = SearchConditonPropName,
                Initializer = "new()",
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = "string",
                PropertyName = SearchResultPropName,
            };
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = typeof(ReferenceDTO).FullName,
                PropertyName = InstanceModelPropName,
                Initializer = $"new() {{ {nameof(ReferenceDTO.AggreageteGuid)} = new Guid(\"{RefTarget.GUID}\") }}",
            };
        }

        internal string SearchConditonPropName => Name;
        internal string SearchResultPropName => Name;
        internal string InstanceModelPropName => Name;

        public override IEnumerable<string> GetInvalidErrors() {
            yield break;
        }

        private protected override void Accept(IMemberVisitor visitor) {
            visitor.Visit(this);
        }
    }
}
