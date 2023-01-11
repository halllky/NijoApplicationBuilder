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

        private bool IsNullable() {
            return UnderlyingPropertyInfo.PropertyType.IsGenericType
                && UnderlyingPropertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        private Type GetPropertyTypeExceptNullable() {
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
        private string GetSearchConditionCSharpTypeName() {
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
        private bool IsRangeSearchCondition() {
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

        private string DbColumnPropName => UnderlyingPropertyInfo.GetCustomAttribute<ColumnAttribute>()?.Name ?? UnderlyingPropertyInfo.Name;

        private string SearchConditonPropName => Name;
        private string SearchResultPropName => Name;
        private string InstanceModelPropName => Name;

        public override string RenderSearchConditionView(ViewRenderingContext context) {
            var type = GetPropertyTypeExceptNullable();
            var nested = context.Nest(SearchConditonPropName);
            if (IsRangeSearchCondition()) {
                // 範囲検索
                var template = new SchalarValueSearchCondition {
                    Type = SchalarValueSearchCondition.E_Type.Range,
                    AspFor = new[] {
                        $"{nested.AspForPath}.{nameof(FromTo.From)}",
                        $"{nested.AspForPath}.{nameof(FromTo.To)}",
                    },
                };
                return template.TransformText();

            } else if (type.IsEnum) {
                // enumドロップダウン
                var template = new SchalarValueSearchCondition {
                    Type = SchalarValueSearchCondition.E_Type.Select,
                    AspFor = new[] { nested.AspForPath },
                    EnumTypeName = GetSearchConditionCSharpTypeName(),
                    Options = IsNullable()
                        ? new[] { KeyValuePair.Create("", "") }
                        : Array.Empty<KeyValuePair<string, string>>(),
                };
                return template.TransformText();

            } else {
                // ただのinput
                var template = new SchalarValueSearchCondition {
                    Type = SchalarValueSearchCondition.E_Type.Input,
                    AspFor = new[] { nested.AspForPath },
                };
                return template.TransformText();
            }
        }

        public override string RenderSearchResultView(ViewRenderingContext context) {
            var nested = context.Nest(SearchResultPropName, isCollection: false);
            return $"<span>@{nested.Path}</span>";
        }

        public override string RenderInstanceView(ViewRenderingContext context) {
            var nested = context.Nest(InstanceModelPropName);
            return $"<input asp-for=\"{nested.AspForPath}\"/>";
        }

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

        public override void MapUIToDB(object uiInstance, object dbInstance, RuntimeContext context) {
            var dbProp = dbInstance.GetType().GetProperty(DbColumnPropName);
            var uiProp = uiInstance.GetType().GetProperty(InstanceModelPropName);

            var value = uiProp.GetValue(uiInstance);
            dbProp.SetValue(dbInstance, value);
        }

        public override void MapDBToUI(object dbInstance, object uiInstance, RuntimeContext context) {
            var dbProp = dbInstance.GetType().GetProperty(DbColumnPropName);
            var uiProp = uiInstance.GetType().GetProperty(InstanceModelPropName);

            var value = dbProp.GetValue(dbInstance);
            uiProp.SetValue(uiInstance, value);
        }

        public override void BuildSelectStatement(SelectStatement selectStatement, object searchCondition, RuntimeContext context, string selectClausePrefix) {
            // SELECT
            var dbEntity = context.DbSchema.GetDbEntity(Owner);
            selectStatement.Select(e => {
                var table = e.GetAlias(dbEntity);
                var column = DbColumnPropName;
                var alias = selectClausePrefix + SearchResultPropName;
                return $"{table}.{column} AS [{alias}]";
            });

            // WHERE
            var prop = searchCondition.GetType().GetProperty(SearchConditonPropName);
            var value = prop.GetValue(searchCondition);
            if (IsRangeSearchCondition()) {
                // 範囲検索
                var fromTo = (FromTo)value;
                if (fromTo.From != null) {
                    selectStatement.Where(e => {
                        var table = e.GetAlias(dbEntity);
                        var column = DbColumnPropName;
                        var param = e.NewParam(fromTo.From);
                        return $"{table}.{column} >= {param}";
                    });
                }
                if (fromTo.To != null) {
                    selectStatement.Where(e => {
                        var table = e.GetAlias(dbEntity);
                        var column = DbColumnPropName;
                        var param = e.NewParam(fromTo.To);
                        return $"{table}.{column} <= {param}";
                    });
                }
            } else if (GetPropertyTypeExceptNullable().IsEnum) {
                // enum
                if (value != null) {
                    selectStatement.Where(e => {
                        var table = e.GetAlias(dbEntity);
                        var column = DbColumnPropName;
                        var param = e.NewParam(value);
                        return $"{table}.{column} = {param}";
                    });
                }
            } else {
                // 文字列部分一致
                if (value is string str && !string.IsNullOrWhiteSpace(str)) {
                    selectStatement.Where(e => {
                        var table = e.GetAlias(dbEntity);
                        var column = DbColumnPropName;
                        var param = e.NewParam($"%{str.Trim().Replace("%", "[%]")}%");
                        return $"{table}.{column} LIKE {param}";
                    });
                }
            }
        }

        public override void MapSearchResultToUI(System.Data.Common.DbDataReader reader, object searchResult, RuntimeContext context, string selectClausePrefix) {
            var prop = searchResult.GetType().GetProperty(SearchResultPropName);
            var value = reader[selectClausePrefix + SearchResultPropName];
            prop.SetValue(searchResult, value == DBNull.Value ? null : value);
        }

        public override void BuildAutoCompleteSelectStatement(SelectStatement selectStatement, string inputText, RuntimeContext context, string selectClausePrefix) {
            // SELECT
            var dbEntity = context.DbSchema.GetDbEntity(Owner);
            selectStatement.Select(e => {
                var table = e.GetAlias(dbEntity);
                var column = DbColumnPropName;
                var alias = selectClausePrefix + SearchResultPropName;
                return $"{table}.{column} AS [{alias}]";
            });

            // WHERE
            if (!string.IsNullOrWhiteSpace(inputText) && IsInstanceName) {
                selectStatement.Where(e => {
                    var table = e.GetAlias(dbEntity);
                    var column = DbColumnPropName;
                    var param = e.NewParam($"%{inputText.Trim().Replace("%", "[%]")}%");
                    return $"{table}.{column} LIKE {param}";
                });
            }
        }

        public override IEnumerable<string> GetInvalidErrors() {
            yield break;
        }
    }

    partial class SchalarValueSearchCondition {
        public enum E_Type {
            Input,
            Range,
            Select,
        }
        public string[] AspFor { get; set; }
        public E_Type Type { get; set; }
        public string EnumTypeName { get; set; }
        public KeyValuePair<string, string>[] Options { get; set; }
    }
}
