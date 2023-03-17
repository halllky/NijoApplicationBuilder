using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core.MemberImpl {
    internal class SchalarValue : AggregateMember {
        internal SchalarValue(Config config, PropertyInfo propertyInfo, Aggregate owner) : base(config, propertyInfo, owner) { }

        internal static bool IsPrimitive(Type type) {
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
        private bool IsNullable() {
            return _underlyingProp.PropertyType.IsGenericType
                && _underlyingProp.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        private Type GetPropertyTypeExceptNullable() {
            return IsNullable()
                ? _underlyingProp.PropertyType.GetGenericArguments()[0]
                : _underlyingProp.PropertyType;
        }
        private string GetCSharpTypeName() {
            var type = GetPropertyTypeExceptNullable();
            string valueTypeName;
            if (type.IsEnum) valueTypeName = type.FullName!;
            else if (type == typeof(string)) valueTypeName = "string";
            else if (type == typeof(bool)) valueTypeName = "bool";
            else if (type == typeof(int)) valueTypeName = "int";
            else if (type == typeof(float)) valueTypeName = "float";
            else if (type == typeof(decimal)) valueTypeName = "decimal";
            else if (type == typeof(DateTime)) valueTypeName = "DateTime";
            else throw new InvalidOperationException($"不正な型: {_underlyingProp.Name} - {type.Name}");

            var question = IsNullable() ? "?" : null;

            return valueTypeName + question;
        }
        private string GetSearchConditionCSharpTypeName() {
            var type = GetPropertyTypeExceptNullable();
            if (type.IsEnum) return type.FullName + "?";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int?";
            if (type == typeof(float)) return "float?";
            if (type == typeof(decimal)) return "decimal?";
            if (type == typeof(DateTime)) return "DateTime?";
            return type.FullName!;
        }
        private bool IsRangeSearchCondition() {
            var type = GetPropertyTypeExceptNullable();
            return new[] { typeof(int), typeof(float), typeof(decimal), typeof(DateTime) }.Contains(type);
        }

        private string DbColumnPropName => _underlyingProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? _underlyingProp.Name;
        internal string SearchConditonPropName => _underlyingProp.Name;
        internal string SearchResultPropName => _underlyingProp.Name;
        internal string InstanceModelPropName => _underlyingProp.Name;

        internal override IEnumerable<Aggregate> GetChildAggregates()
        {
            yield break;
        }

        internal override void BuildSearchMethod(SearchMethodDTO method)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<string> GetInstanceKeysFromInstanceModel(object uiInstance)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<string> GetInstanceKeysFromSearchResult(object searchResult)
        {
            throw new NotImplementedException();
        }

        internal override void MapDbToUi(object dbInstance, object uiInstance)
        {
            throw new NotImplementedException();
        }

        internal override void MapUiToDb(object uiInstance, object dbInstance)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<RenderedProerty> ToDbEntityMember() {
            yield return new RenderedProerty {
                Virtual = false,
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = DbColumnPropName,
                Initializer = null,
            };
        }

        internal override IEnumerable<RenderedProerty> ToInstanceModelMember()
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<RenderedProerty> ToSearchConditionMember()
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<RenderedProerty> ToSearchResultMember()
        {
            throw new NotImplementedException();
        }
    }
}
