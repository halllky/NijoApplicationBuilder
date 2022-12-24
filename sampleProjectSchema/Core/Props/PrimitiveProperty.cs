using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using haldoc.Core.Dto;
using haldoc.Schema;
using haldoc.SqlQuery;

namespace haldoc.Core.Props {
    public class PrimitiveProperty : AggregatePropBase, SqlQuery.ISearchConditionHandler {

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield break;
        }

        private string GetCSharpTypeName() {
            if (UnderlyingPropInfo.PropertyType == typeof(string)) return "string";
            else if (UnderlyingPropInfo.PropertyType == typeof(bool)) return "bool";
            else if (UnderlyingPropInfo.PropertyType == typeof(int)) return "int";
            else if (UnderlyingPropInfo.PropertyType == typeof(float)) return "float";
            else if (UnderlyingPropInfo.PropertyType == typeof(decimal)) return "decimal";
            else if (UnderlyingPropInfo.PropertyType.IsEnum) return UnderlyingPropInfo.PropertyType.FullName;

            else if (UnderlyingPropInfo.PropertyType.IsGenericType
                && UnderlyingPropInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                var generic = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0];
                if (generic == typeof(string)) return "string?";
                else if (generic == typeof(bool)) return "bool?";
                else if (generic == typeof(int)) return "int?";
                else if (generic == typeof(float)) return "float?";
                else if (generic == typeof(decimal)) return "decimal?";
                else if (generic.IsEnum) return UnderlyingPropInfo.PropertyType.GetGenericArguments()[0].FullName + "?";
            }

            return UnderlyingPropInfo.PropertyType.FullName;
        }

        public override IEnumerable<PropertyTemplate> ToDbEntityProperty() {
            yield return new PropertyTemplate {
                PropertyName = this.Name,
                CSharpTypeName = GetCSharpTypeName(),
            };
        }

        public override IEnumerable<PropertyTemplate> ToSearchConditionDtoProperty() {
            string typeName;
            var fromto = typeof(FromTo).FullName;
            if (UnderlyingPropInfo.PropertyType == typeof(string)) typeName = "string";
            else if (UnderlyingPropInfo.PropertyType == typeof(bool)) typeName = "bool";
            else if (UnderlyingPropInfo.PropertyType == typeof(int)) typeName = $"{fromto}<int?>";
            else if (UnderlyingPropInfo.PropertyType == typeof(float)) typeName = $"{fromto}<float?>";
            else if (UnderlyingPropInfo.PropertyType == typeof(decimal)) typeName = $"{fromto}<decimal?>";
            else if (UnderlyingPropInfo.PropertyType.IsEnum) typeName = UnderlyingPropInfo.PropertyType.FullName;

            else if (UnderlyingPropInfo.PropertyType.IsGenericType
                && UnderlyingPropInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                var generic = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0];
                if (generic == typeof(string)) typeName = "string?";
                else if (generic == typeof(bool)) typeName = "bool?";
                else if (generic == typeof(int)) typeName = $"{fromto}<int?>";
                else if (generic == typeof(float)) typeName = $"{fromto}<float?>";
                else if (generic == typeof(decimal)) typeName = $"{fromto}<decimal?>";
                else if (generic.IsEnum) typeName = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0].FullName + "?";
                else typeName = generic.IsValueType ? $"{generic.FullName}?" : generic.FullName;
            } else {
                typeName = UnderlyingPropInfo.PropertyType.FullName;
            }

            yield return new PropertyTemplate {
                PropertyName = this.Name,
                CSharpTypeName = typeName,
            };
        }
        public override IEnumerable<string> GenerateSearchConditionLayout(string modelPath) {
            var aspFor = string.IsNullOrEmpty(modelPath) ? Name : $"{modelPath}.{Name}";
            if (UnderlyingPropInfo.PropertyType == typeof(string)
                || UnderlyingPropInfo.PropertyType == typeof(bool)) {
                yield return $"<input asp-for=\"{aspFor}\" />";

            } else if (UnderlyingPropInfo.PropertyType == typeof(int)
                || UnderlyingPropInfo.PropertyType == typeof(int?)
                || UnderlyingPropInfo.PropertyType == typeof(float)
                || UnderlyingPropInfo.PropertyType == typeof(float?)
                || UnderlyingPropInfo.PropertyType == typeof(decimal)
                || UnderlyingPropInfo.PropertyType == typeof(decimal?)) {
                yield return $"<div>";
                yield return $"    <input asp-for=\"{aspFor}.{nameof(FromTo.From)}\" />";
                yield return $"     〜 ";
                yield return $"    <input asp-for=\"{aspFor}.{nameof(FromTo.To)}\" />";
                yield return $"</div>";

            } else if (UnderlyingPropInfo.PropertyType.IsEnum) {
                var enumTypeName = UnderlyingPropInfo.PropertyType.FullName;
                yield return $"<select asp-for=\"{aspFor}\" asp-items=\"@Html.GetEnumSelectList(typeof({enumTypeName}))\">";
                yield return $"</select>";

            } else if (UnderlyingPropInfo.PropertyType.IsGenericType
                && UnderlyingPropInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                && UnderlyingPropInfo.PropertyType.GetGenericArguments()[0].IsEnum) {
                var enumTypeName = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0].FullName;
                yield return $"<select asp-for=\"{aspFor}\" asp-items=\"@Html.GetEnumSelectList(typeof({enumTypeName}))\">";
                yield return $"    <option selected=\"selected\" value=\"\"></option>";
                yield return $"</select>";
            }
        }

        public override IEnumerable<PropertyTemplate> ToListItemModel() {
            yield return new PropertyTemplate {
                PropertyName = this.Name,
                CSharpTypeName = GetCSharpTypeName(),
            };
        }

        public override IEnumerable<PropertyTemplate> ToInstanceDtoProperty() {
            return ToDbEntityProperty();
        }

        public override string RenderSingleView(AggregateInstanceBuildContext renderingContext) {
            renderingContext.Push(Name);

            var template = new PrimitivePropertyInstance {
                Property = this,
                RenderingContext = renderingContext,
            };
            var code = string.Join(Environment.NewLine, template
                .TransformText()
                .Split(Environment.NewLine)
                .Select((line, index) => index == 0
                    ? line // 先頭行だけは呼び出し元ttファイル内のインデントがそのまま反映されるので
                    : renderingContext.CurrentIndent + line));

            renderingContext.Pop();
            return code;
        }

        object ISearchConditionHandler.Deserialize(string serialized) {
            if (UnderlyingPropInfo.PropertyType == typeof(string)) return serialized;
            else if (UnderlyingPropInfo.PropertyType == typeof(bool)) return bool.TryParse(serialized, out var b) ? b : default(bool);
            else if (UnderlyingPropInfo.PropertyType == typeof(int)) return int.TryParse(serialized, out var i) ? i : default(int);
            else if (UnderlyingPropInfo.PropertyType == typeof(float)) return float.TryParse(serialized, out var f) ? f : default(float);
            else if (UnderlyingPropInfo.PropertyType == typeof(decimal)) return decimal.TryParse(serialized, out var d) ? d : default(decimal);
            else if (UnderlyingPropInfo.PropertyType.IsEnum) return Enum.TryParse(UnderlyingPropInfo.PropertyType, serialized, out var e) ? e : Activator.CreateInstance(UnderlyingPropInfo.PropertyType);

            else if (UnderlyingPropInfo.PropertyType.IsGenericType
                && UnderlyingPropInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                var generic = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0];
                if (generic == typeof(string)) return serialized;
                else if (generic == typeof(bool)) return bool.TryParse(serialized, out var b) ? b : null;
                else if (generic == typeof(int)) return int.TryParse(serialized, out var i) ? i : null;
                else if (generic == typeof(float)) return float.TryParse(serialized, out var f) ? f : null;
                else if (generic == typeof(decimal)) return decimal.TryParse(serialized, out var d) ? d : null;
                else if (generic.IsEnum) return Enum.TryParse(UnderlyingPropInfo.PropertyType, serialized, out var e) ? e : null;
            }
            throw new InvalidOperationException();
        }

        string ISearchConditionHandler.Serialize(object searchCondition) {
            return searchCondition?.ToString();
        }

        IEnumerable<string> ISearchConditionHandler.GenerateSearchConditionLayout(string modelName) {
            if (UnderlyingPropInfo.PropertyType == typeof(string)
                || UnderlyingPropInfo.PropertyType == typeof(bool)) {
                yield return $"<input asp-for=\"{modelName}\" />";

            } else if (UnderlyingPropInfo.PropertyType == typeof(int)
                || UnderlyingPropInfo.PropertyType == typeof(int?)
                || UnderlyingPropInfo.PropertyType == typeof(float)
                || UnderlyingPropInfo.PropertyType == typeof(float?)
                || UnderlyingPropInfo.PropertyType == typeof(decimal)
                || UnderlyingPropInfo.PropertyType == typeof(decimal?)) {
                yield return $"<div>";
                yield return $"    <input asp-for=\"{modelName}.{nameof(FromTo.From)}\" />";
                yield return $"     〜 ";
                yield return $"    <input asp-for=\"{modelName}.{nameof(FromTo.To)}\" />";
                yield return $"</div>";

            } else if (UnderlyingPropInfo.PropertyType.IsEnum) {
                yield return $"<select asp-for=\"{modelName}\" asp-items=\"Html.GetEnumSelectList<{UnderlyingPropInfo.PropertyType.FullName}>()\">";
                yield return $"</select>";

            } else if (UnderlyingPropInfo.PropertyType.IsGenericType
                && UnderlyingPropInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                && UnderlyingPropInfo.PropertyType.GetGenericArguments()[0].IsEnum) {
                yield return $"<select asp-for=\"{modelName}\" asp-items=\"Html.GetEnumSelectList<{UnderlyingPropInfo.PropertyType.FullName}>()\">";
                yield return $"<option selected=\"selected\" value=\"\"></option>";
                yield return $"</select>";
            }
        }

        void ISearchConditionHandler.AppendWhereClause(string tableAlias, object searchCondition, QueryBuilderContext context) {
        }

        internal static bool IsPrimitive(Type type) {
            if (type == typeof(string)) return true;
            if (type == typeof(bool)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(decimal)) return true;
            if (type.IsEnum) return true;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                var generic = type.GetGenericArguments()[0];
                if (generic == typeof(string)) return true;
                if (generic == typeof(bool)) return true;
                if (generic == typeof(int)) return true;
                if (generic == typeof(float)) return true;
                if (generic == typeof(decimal)) return true;
                if (generic.IsEnum) return true;
            }

            return false;
        }
    }

    partial class PrimitivePropertyInstance {
        public PrimitiveProperty Property { get; init; }
        public AggregateInstanceBuildContext RenderingContext { get; init; }
    }
}
