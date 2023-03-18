using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core.Members;
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

        internal override IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        internal override void BuildSearchMethod(SearchMethodDTO method) {
            // SELECT句の組み立て
            method.SelectClause.Add($"{SearchResultPropName} = {method.SelectLambdaVarName}.{SearchResultPropName},");

            // WHERE句の組み立て
            var type = GetPropertyTypeExceptNullable();
            var query = method.QueryVarName;
            if (IsRangeSearchCondition()) {
                var valueFrom = $"{method.ParamVarName}.{SearchConditonPropName}.{nameof(FromTo.From)}";
                var valueTo = $"{method.ParamVarName}.{SearchConditonPropName}.{nameof(FromTo.To)}";
                method.WhereClause.Add($"if ({valueFrom} != null) {{");
                method.WhereClause.Add($"    {query} = {query}.Where(e => e.{DbColumnPropName} >= {valueFrom});");
                method.WhereClause.Add($"}}");
                method.WhereClause.Add($"if ({valueTo} != null) {{");
                method.WhereClause.Add($"    {query} = {query}.Where(e => e.{DbColumnPropName} <= {valueTo});");
                method.WhereClause.Add($"}}");

            } else if (type == typeof(string)) {
                var value = $"{method.ParamVarName}.{SearchConditonPropName}";
                method.WhereClause.Add($"if (!string.{nameof(string.IsNullOrWhiteSpace)}({value})) {{");
                method.WhereClause.Add($"    {query} = {query}.Where(e => e.{DbColumnPropName}.{nameof(string.Contains)}({value}));");
                method.WhereClause.Add($"}}");

            } else {
                var value = $"{method.ParamVarName}.{SearchConditonPropName}";
                method.WhereClause.Add($"if ({value} != null) {{");
                method.WhereClause.Add($"    {query} = {query}.Where(e => e.{DbColumnPropName} == {value});");
                method.WhereClause.Add($"}}");
            }
        }

        internal override void RenderMvcSearchConditionView(RenderingContext context) {
            var type = GetPropertyTypeExceptNullable();

            if (IsRangeSearchCondition()) {
                // 範囲検索
                var from = context.ObjectPath
                    .Nest(SearchConditonPropName)
                    .Nest(nameof(FromTo.From))
                    .AspForPath;
                var to = context.ObjectPath
                    .Nest(SearchConditonPropName)
                    .Nest(nameof(FromTo.To))
                    .AspForPath;
                context.Template.WriteLine($"<input asp-for=\"{from}\" class=\"border\" />");
                context.Template.WriteLine($"〜");
                context.Template.WriteLine($"<input asp-for=\"{to}\" class=\"border\" />");

            } else if (type.IsEnum) {
                // enumドロップダウン
                var searchCondition = context.ObjectPath
                    .Nest(SearchConditonPropName)
                    .AspForPath;
                var enumTypeName = GetSearchConditionCSharpTypeName();
                var options = new List<KeyValuePair<string, string>>();
                if (IsNullable()) options.Add(KeyValuePair.Create("", ""));

                context.Template.WriteLine($"<select asp-for=\"{searchCondition}\" asp-items=\"@Html.GetEnumSelectList(typeof({enumTypeName}))\">");
                context.Template.PushIndent("    ");
                foreach (var opt in options) {
                    context.Template.WriteLine($"<option selected=\"selected\" value=\"{opt.Key}\">");
                    context.Template.WriteLine($"    {opt.Value}");
                    context.Template.WriteLine($"</option>");
                }
                context.Template.PopIndent();
                context.Template.WriteLine($"</select>");

            } else {
                // ただのinput
                var searchCondition = context.ObjectPath
                    .Nest(SearchConditonPropName)
                    .AspForPath;
                context.Template.WriteLine($"<input asp-for=\"{searchCondition}\" class=\"border\" />");
            }
        }

        internal override void RenderAspNetMvcPartialView(RenderingContext context) {
            var value = context.ObjectPath
                .Nest(InstanceModelPropName)
                .AspForPath;
            context.Template.WriteLine($"<input asp-for=\"{value}\" class=\"border\" />");
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

        internal override IEnumerable<RenderedProperty> ToDbEntityMember() {
            yield return new RenderedProperty {
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = DbColumnPropName,
            };
        }

        internal override IEnumerable<RenderedProperty> ToInstanceModelMember() {
            yield return new RenderedProperty {
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = InstanceModelPropName,
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchConditionMember() {
            var type = GetPropertyTypeExceptNullable();
            if (new[] { typeof(int), typeof(float), typeof(decimal), typeof(DateTime) }.Contains(type)) {
                // 範囲検索
                yield return new RenderedProperty {
                    CSharpTypeName = $"{typeof(FromTo).Namespace}.{nameof(FromTo)}<{GetSearchConditionCSharpTypeName()}>",
                    PropertyName = SearchConditonPropName,
                    Initializer = "new()",
                };

            } else if (type.IsEnum) {
                // enumドロップダウン
                yield return new RenderedProperty {
                    CSharpTypeName = type.FullName ?? throw new InvalidOperationException($"type.FullNameを取得できない: {_underlyingProp.Name}"),
                    PropertyName = SearchConditonPropName,
                };

            } else {
                // ただのinput
                yield return new RenderedProperty {
                    CSharpTypeName = GetSearchConditionCSharpTypeName(),
                    PropertyName = SearchConditonPropName,
                };
            }
        }

        internal override IEnumerable<RenderedProperty> ToSearchResultMember() {
            yield return new RenderedProperty {
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = SearchResultPropName,
            };
        }
    }
}
