using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.CodeRendering;
using HalApplicationBuilder.Runtime;
using HalApplicationBuilder.Serialized;

namespace HalApplicationBuilder.Core.MemberImpl {
    internal class SchalarValue : AggregateMember {

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

        internal SchalarValue(Config config, string displayName, bool isPrimary, Aggregate owner, PropertyInfo prop) : base(config, displayName, isPrimary, owner) {
            _propertyType = prop.PropertyType;

            if (prop.PropertyType.IsGenericType && prop.PropertyType == typeof(Nullable<>))
                IsNullable = true;
            else if (prop.PropertyType == typeof(string)
                // "string?" のnull許容演算子はランタイム時に自動生成されるため名称で探すしかない
                && prop.GetCustomAttributesData().Any(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute"))
                IsNullable = true;
            else
                IsNullable = false;
        }
        internal SchalarValue(Config config, string displayName, bool isPrimary, Aggregate owner, Type propertyTypeWithoutNullable, bool isNullable) : base(config, displayName, isPrimary, owner) {
            _propertyType = isNullable && propertyTypeWithoutNullable != typeof(string)
                ? typeof(Nullable<>).MakeGenericType(propertyTypeWithoutNullable)
                : propertyTypeWithoutNullable;
            IsNullable = isNullable;
        }

        private readonly Type _propertyType;

        private bool IsNullable { get; }
        private Type GetPropertyTypeExceptNullable() {
            if (_propertyType.IsGenericType && _propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return _propertyType.GetGenericArguments()[0];
            else
                return _propertyType;
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
            else throw new InvalidOperationException($"不正な型: {DisplayName} - {type.Name}");

            var question = IsNullable ? "?" : null;

            return valueTypeName + question;
        }
        private string GetTypeScriptTypeName() {
            var type = GetPropertyTypeExceptNullable();
            string valueTypeName;
            if (type.IsEnum) valueTypeName = string.Join(" | ", Enum.GetNames(type).Select(name => $"'{name}'"));
            else if (type == typeof(string)) valueTypeName = "string";
            else if (type == typeof(bool)) valueTypeName = "boolean";
            else if (type == typeof(int)) valueTypeName = "number";
            else if (type == typeof(float)) valueTypeName = "number";
            else if (type == typeof(decimal)) valueTypeName = "number";
            else if (type == typeof(DateTime)) valueTypeName = "Date";
            else throw new InvalidOperationException($"不正な型: {DisplayName} - {type.Name}");

            var question = IsNullable ? " | undefined" : null;

            return valueTypeName + question;
        }

        private string GetSearchConditionCSharpTypeName() {
            var type = GetPropertyTypeExceptNullable();
            if (type.IsEnum) return type.FullName + "?";
            if (type == typeof(string)) return "string?";
            if (type == typeof(bool)) return "bool?";
            if (type == typeof(int)) return "int?";
            if (type == typeof(float)) return "float?";
            if (type == typeof(decimal)) return "decimal?";
            if (type == typeof(DateTime)) return "DateTime?";
            return type.FullName!;
        }
        private string GetSearchConditionTypeScriptTypeName() {
            var type = GetPropertyTypeExceptNullable();
            if (type.IsEnum) return string.Join(" | ", Enum.GetNames(type).Select(name => $"'{name}'"));
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(int)) return "number | undefined";
            if (type == typeof(float)) return "number | undefined";
            if (type == typeof(decimal)) return "number | undefined";
            if (type == typeof(DateTime)) return "Date | undefined";
            return type.FullName!;
        }

        private bool IsRangeSearchCondition() {
            var type = GetPropertyTypeExceptNullable();
            return new[] { typeof(int), typeof(float), typeof(decimal), typeof(DateTime) }.Contains(type);
        }

        private string DbColumnPropName => DisplayName;
        internal string SearchConditonPropName => DisplayName;
        internal string SearchResultPropName => DisplayName;
        internal string InstanceModelPropName => DisplayName;

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
                if (IsNullable) options.Add(KeyValuePair.Create("", ""));

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
        internal override void RenderReactComponent(RenderingContext context) {
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
                context.Template.WriteLine($"<input type=\"text\" className=\"border\" />");
                context.Template.WriteLine($"〜");
                context.Template.WriteLine($"<input type=\"text\" className=\"border\" />");

            } else if (type.IsEnum) {
                // enumドロップダウン
                var options = new List<KeyValuePair<string, string>>();
                if (IsNullable) options.Add(KeyValuePair.Create("", ""));

                context.Template.WriteLine($"<select className=\"border\">");
                foreach (var opt in options) {
                    context.Template.WriteLine($"    <option selected=\"selected\" value=\"{opt.Key}\">");
                    context.Template.WriteLine($"        {opt.Value}");
                    context.Template.WriteLine($"    </option>");
                }
                context.Template.WriteLine($"</select>");

            } else {
                // ただのinput
                var searchCondition = context.ObjectPath
                    .Nest(SearchConditonPropName)
                    .AspForPath;
                context.Template.WriteLine($"<input type=\"text\" className=\"border\" />");
            }
        }

        internal override void RenderAspNetMvcPartialView(RenderingContext context) {
            var value = context.ObjectPath
                .Nest(InstanceModelPropName)
                .AspForPath;
            context.Template.WriteLine($"<input asp-for=\"{value}\" class=\"border\" />");
        }

        internal override object? GetInstanceKeyFromDbInstance(object dbInstance) {
            var prop = dbInstance.GetType().GetProperty(DbColumnPropName);
            if (prop == null) throw new ArgumentException(null, nameof(dbInstance));
            return prop.GetValue(dbInstance);
        }
        internal override void MapInstanceKeyToDbInstance(object? instanceKey, object dbInstance) {
            var prop = dbInstance.GetType().GetProperty(DbColumnPropName);
            if (prop == null) throw new ArgumentException(null, nameof(dbInstance));
            prop.SetValue(dbInstance, instanceKey);
        }

        internal override object? GetInstanceKeyFromUiInstance(object uiInstance) {
            var prop = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (prop == null) throw new ArgumentException(null, nameof(uiInstance));
            return prop.GetValue(uiInstance);
        }
        internal override void MapInstanceKeyToUiInstance(object? instanceKey, object uiInstance) {
            var prop = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (prop == null) throw new ArgumentException(null, nameof(uiInstance));
            prop.SetValue(uiInstance, instanceKey);
        }

        internal override object? GetInstanceKeyFromSearchResult(object searchResult) {
            var prop = searchResult.GetType().GetProperty(SearchResultPropName);
            if (prop == null) throw new ArgumentException(null, nameof(searchResult));
            return prop.GetValue(searchResult);
        }
        internal override void MapInstanceKeyToSearchResult(object? instanceKey, object searchResult) {
            var prop = searchResult.GetType().GetProperty(SearchResultPropName);
            if (prop == null) throw new ArgumentException(null, nameof(searchResult));
            prop.SetValue(searchResult, instanceKey);
        }

        internal override object? GetInstanceKeyFromAutoCompleteItem(object autoCompelteItem) {
            var prop = autoCompelteItem.GetType().GetProperty(DbColumnPropName);
            if (prop == null) throw new ArgumentException(null, nameof(autoCompelteItem));
            return prop.GetValue(autoCompelteItem);
        }
        internal override void MapInstanceKeyToAutoCompleteItem(object? instanceKey, object autoCompelteItem) {
            var prop = autoCompelteItem.GetType().GetProperty(DbColumnPropName);
            if (prop == null) throw new ArgumentException(null, nameof(autoCompelteItem));
            prop.SetValue(autoCompelteItem, instanceKey);
        }

        internal override void MapDbToUi(object dbInstance, object uiInstance, IInstanceConvertingContext context) {
            var dbProp = dbInstance.GetType().GetProperty(DbColumnPropName);
            var uiProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (dbProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiProp == null) throw new ArgumentException(null, nameof(uiInstance));

            var value = dbProp.GetValue(dbInstance);
            uiProp.SetValue(uiInstance, value);
        }

        internal override void MapUiToDb(object uiInstance, object dbInstance, IInstanceConvertingContext context) {
            var dbProp = dbInstance.GetType().GetProperty(DbColumnPropName);
            var uiProp = uiInstance.GetType().GetProperty(InstanceModelPropName);
            if (dbProp == null) throw new ArgumentException(null, nameof(dbInstance));
            if (uiProp == null) throw new ArgumentException(null, nameof(uiInstance));

            var value = uiProp.GetValue(uiInstance);
            dbProp.SetValue(dbInstance, value);
        }

        internal override IEnumerable<RenderedProperty> ToDbEntityMember() {
            yield return new RenderedProperty {
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = DbColumnPropName,
                Nullable = IsNullable,

                TypeScriptTypeName = GetTypeScriptTypeName(),
            };
        }

        internal override IEnumerable<RenderedProperty> ToInstanceModelMember() {
            yield return new RenderedProperty {
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = InstanceModelPropName,

                TypeScriptTypeName = GetTypeScriptTypeName(),
            };
        }

        internal override IEnumerable<RenderedProperty> ToSearchConditionMember() {
            var type = GetPropertyTypeExceptNullable();
            if (new[] { typeof(int), typeof(float), typeof(decimal), typeof(DateTime) }.Contains(type)) {
                // 範囲検索
                var tsType = GetSearchConditionTypeScriptTypeName();
                yield return new RenderedProperty {
                    CSharpTypeName = $"{typeof(FromTo).Namespace}.{nameof(FromTo)}<{GetSearchConditionCSharpTypeName()}>",
                    PropertyName = SearchConditonPropName,
                    Initializer = "new()",

                    TypeScriptTypeName = $"{{ from: {tsType} ,to: {tsType} }}",
                };

            } else if (type.IsEnum) {
                // enumドロップダウン
                yield return new RenderedProperty {
                    CSharpTypeName = type.FullName ?? throw new InvalidOperationException($"type.FullNameを取得できない: {DisplayName}"),
                    TypeScriptTypeName = GetTypeScriptTypeName(),
                    PropertyName = SearchConditonPropName,
                };

            } else {
                // ただのinput
                yield return new RenderedProperty {
                    CSharpTypeName = GetSearchConditionCSharpTypeName(),
                    TypeScriptTypeName = GetTypeScriptTypeName(),
                    PropertyName = SearchConditonPropName,
                };
            }
        }

        internal override IEnumerable<RenderedProperty> ToSearchResultMember() {
            yield return new RenderedProperty {
                CSharpTypeName = GetCSharpTypeName(),
                TypeScriptTypeName = GetTypeScriptTypeName(),
                PropertyName = SearchResultPropName,
            };
        }

        private const string ENUM_PREFIX = "enum::";
        internal static Type? TryParseTypeName(string kind) {
            if (kind == "string") return typeof(string);
            if (kind == "bool") return typeof(bool);
            if (kind == "int") return typeof(int);
            if (kind == "float") return typeof(float);
            if (kind == "decimal") return typeof(decimal);
            if (kind == "datetime") return typeof(DateTime);
            if (kind.StartsWith(ENUM_PREFIX)) {
                var assembly = Assembly.GetEntryAssembly(); // TODO: enumは必ずしもEntryAssemblyにあるとは限らない
                if (assembly == null) return null;
                var enumType = assembly.GetType(kind.Substring(ENUM_PREFIX.Length));
                return enumType;
            }
            return null;
        }
        internal override MemberJson ToJson() {
            string kind;
            var type = GetPropertyTypeExceptNullable();
            if (type == typeof(string)) kind = "string";
            else if (type == typeof(bool)) kind = "bool";
            else if (type == typeof(int)) kind = "int";
            else if (type == typeof(float)) kind = "float";
            else if (type == typeof(decimal)) kind = "decimal";
            else if (type == typeof(DateTime)) kind = "datetime";
            else if (type.IsEnum) kind = ENUM_PREFIX + type.FullName;
            else throw new InvalidOperationException();

            return new MemberJson {
                Kind = kind,
                Name = this.DisplayName,
                IsPrimary = this.IsPrimary ? true : null,
                IsNullable = this.IsNullable,
            };
        }
    }
}
