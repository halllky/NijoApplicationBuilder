using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.DBModel;
using HalApplicationBuilder.Core.UIModel;
using HalApplicationBuilder.Core.Runtime;
using HalApplicationBuilder.DotnetEx;

namespace HalApplicationBuilder.Core.Members {
    internal class SchalarValue : AggregateMemberBase {
        internal SchalarValue(PropertyInfo propertyInfo, Aggregate owner, IServiceProvider serviceProvider)
            : base(propertyInfo, owner, serviceProvider) { }

        public override bool IsCollection => false;

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        internal bool IsNullable() {
            return UnderlyingPropertyInfo.PropertyType.IsGenericType
                && UnderlyingPropertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        internal Type GetPropertyTypeExceptNullable() {
            return IsNullable()
                ? UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0]
                : UnderlyingPropertyInfo.PropertyType;
        }
        private string GetCSharpTypeName() {
            var type = GetPropertyTypeExceptNullable();
            string valueTypeName = null;
            if (type.IsEnum) valueTypeName = type.FullName;
            else if (type == typeof(string)) valueTypeName = "string";
            else if (type == typeof(bool)) valueTypeName = "bool";
            else if (type == typeof(int)) valueTypeName = "int";
            else if (type == typeof(float)) valueTypeName = "float";
            else if (type == typeof(decimal)) valueTypeName = "decimal";
            else if (type == typeof(DateTime)) valueTypeName = "DateTime";

            var question = IsNullable() ? "?" : null;

            return valueTypeName + question;
        }
        internal string GetSearchConditionCSharpTypeName() {
            var type = GetPropertyTypeExceptNullable();
            if (type.IsEnum) return  type.FullName + "?";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int?";
            if (type == typeof(float)) return "float?";
            if (type == typeof(decimal)) return "decimal?";
            if (type == typeof(DateTime)) return "DateTime?";
            return type.FullName;
        }
        internal bool IsRangeSearchCondition() {
            var type = GetPropertyTypeExceptNullable();
            return new[] { typeof(int), typeof(float), typeof(decimal), typeof(DateTime) }.Contains(type);
        }

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            yield return new DbColumn {
                Virtual = false,
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = DbColumnPropName,
                Initializer = null,
            };
        }

        public override IEnumerable<MvcModelProperty> CreateSearchConditionModels() {
            var type = GetPropertyTypeExceptNullable();
            if (new[] { typeof(int), typeof(float), typeof(decimal), typeof(DateTime) }.Contains(type)) {
                // 範囲検索
                yield return new MvcModelProperty {
                    CSharpTypeName = $"{typeof(FromTo).Namespace}.{nameof(FromTo)}<{GetSearchConditionCSharpTypeName()}>",
                    PropertyName = SearchConditonPropName,
                    Initializer = "new()",
                };

            } else if (type.IsEnum) {
                // enumドロップダウン
                yield return new MvcModelProperty {
                    CSharpTypeName = type.FullName,
                    PropertyName = SearchConditonPropName,
                };

            } else {
                // ただのinput
                yield return new MvcModelProperty {
                    CSharpTypeName = GetSearchConditionCSharpTypeName(),
                    PropertyName = SearchConditonPropName,
                };
            }
        }

        public override IEnumerable<MvcModelProperty> CreateSearchResultModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = SearchResultPropName,
            };
        }

        public override IEnumerable<MvcModelProperty> CreateInstanceModels() {
            yield return new MvcModelProperty {
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = InstanceModelPropName,
            };
        }

        internal string DbColumnPropName => UnderlyingPropertyInfo.GetCustomAttribute<ColumnAttribute>()?.Name ?? UnderlyingPropertyInfo.Name;

        internal string SearchConditonPropName => Name;
        internal string SearchResultPropName => Name;
        internal string InstanceModelPropName => Name;

        public static bool IsPrimitive(Type type) {
            if (type == typeof(string)) return true;
            if (type == typeof(bool)) return true;
            if (type == typeof(bool?)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(int?)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(float?)) return true;
            if (type == typeof(decimal)) return true;
            if (type == typeof(decimal?)) return true;
            if (type == typeof(DateTime)) return true;
            if (type == typeof(DateTime?)) return true;
            if (type.IsEnum) return true;

            return false;
        }

        public override IEnumerable<string> GetInvalidErrors() {
            yield break;
        }

        private protected override void Accept(IMemberVisitor visitor) {
            visitor.Visit(this);
        }
    }
}
