﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HalApplicationBuilder.Core;
using HalApplicationBuilder.EntityFramework;
using HalApplicationBuilder.Runtime;

namespace HalApplicationBuilder.MembersImpl {
    internal class SchalarValue : AggregateMemberBase {
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

        public override IEnumerable<DbColumn> ToDbColumnModel() {
            yield return new DbColumn {
                Virtual = false,
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = UnderlyingPropertyInfo.GetCustomAttribute<ColumnAttribute>()?.Name
                    ?? UnderlyingPropertyInfo.Name,
                Initializer = null,
            };
        }

        public override IEnumerable<AspNetMvc.MvcModelProperty> CreateSearchConditionModels(IAggregateMember member) {
            var propName = UnderlyingPropertyInfo.Name;
            var type = GetPropertyTypeExceptNullable();
            if (new[] { typeof(int), typeof(float), typeof(decimal), typeof(DateTime) }.Contains(type)) {
                // 範囲検索
                yield return new AspNetMvc.MvcModelProperty {
                    CSharpTypeName = $"{typeof(FromTo<>).Namespace}.{nameof(FromTo<object>)}<{GetSearchConditionCSharpTypeName()}>",
                    PropertyName = propName,
                };

            } else if (type.IsEnum) {
                // enumドロップダウン
                yield return new AspNetMvc.MvcModelProperty {
                    CSharpTypeName = type.FullName,
                    PropertyName = propName,
                };

            } else {
                // ただのinput
                yield return new AspNetMvc.MvcModelProperty {
                    CSharpTypeName = GetSearchConditionCSharpTypeName(),
                    PropertyName = propName,
                };
            }
        }

        public override IEnumerable<AspNetMvc.MvcModelProperty> CreateSearchResultModels(IAggregateMember member) {
            yield return new AspNetMvc.MvcModelProperty {
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = UnderlyingPropertyInfo.Name,
            };
        }

        public override IEnumerable<AspNetMvc.MvcModelProperty> CreateInstanceModels(IAggregateMember member) {
            var propertyName = UnderlyingPropertyInfo.Name;
            yield return new AspNetMvc.MvcModelProperty {
                CSharpTypeName = GetCSharpTypeName(),
                PropertyName = propertyName,
            };
        }

        internal override string RenderSearchConditionView(AspNetMvc.ViewRenderingContext context) {
            var propName = SearchConditionModels.Single().PropertyName;
            var type = GetPropertyTypeExceptNullable();
            var nestedContext = context.Nest(propName, isCollection: false);
            if (new[] { typeof(int), typeof(float), typeof(decimal), typeof(DateTime) }.Contains(type)) {
                // 範囲検索
                var template = new SchalarValueSearchCondition {
                    Type = SchalarValueSearchCondition.E_Type.Range,
                    AspFor = new[] {
                        $"{nestedContext.AspForPath}.{nameof(FromTo<object>.From)}",
                        $"{nestedContext.AspForPath}.{nameof(FromTo<object>.To)}",
                    },
                };
                return template.TransformText();

            } else if (type.IsEnum) {
                // enumドロップダウン
                var template = new SchalarValueSearchCondition {
                    Type = SchalarValueSearchCondition.E_Type.Select,
                    AspFor = new[] { nestedContext.AspForPath },
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
                    AspFor = new[] { nestedContext.AspForPath },
                };
                return template.TransformText();
            }
        }

        internal override string RenderSearchResultView(AspNetMvc.ViewRenderingContext context) {
            var propertyName = SearchResultModels.Single().PropertyName;
            var nested = context.Nest(propertyName, isCollection: false);
            return $"<span>@{nested.Path}</span>";
        }

        internal override string RenderInstanceView(AspNetMvc.ViewRenderingContext context) {
            var propertyName = InstanceModels.Single().PropertyName;
            var nested = context.Nest(propertyName);
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
