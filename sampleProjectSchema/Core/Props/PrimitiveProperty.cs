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

        public override IEnumerable<PropertyTemplate> ToDbColumnModel() {
            var name = UnderlyingPropInfo.Name;

            if (UnderlyingPropInfo.PropertyType == typeof(string)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "string" };
            else if (UnderlyingPropInfo.PropertyType == typeof(bool)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "bool" };
            else if (UnderlyingPropInfo.PropertyType == typeof(int)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "int" };
            else if (UnderlyingPropInfo.PropertyType == typeof(float)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "float" };
            else if (UnderlyingPropInfo.PropertyType == typeof(decimal)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "decimal" };
            else if (UnderlyingPropInfo.PropertyType.IsEnum) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = UnderlyingPropInfo.PropertyType.FullName };

            else if (UnderlyingPropInfo.PropertyType.IsGenericType
                && UnderlyingPropInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                var generic = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0];
                if (generic == typeof(string)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "string?" };
                else if (generic == typeof(bool)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "bool?" };
                else if (generic == typeof(int)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "int?" };
                else if (generic == typeof(float)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "float?" };
                else if (generic == typeof(decimal)) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = "decimal?" };
                else if (generic.IsEnum) yield return new PropertyTemplate { PropertyName = name, CSharpTypeName = UnderlyingPropInfo.PropertyType.GetGenericArguments()[0].FullName + "?" };
            }
        }

        public override IEnumerable<PropertyTemplate> ToListItemMember() {
            yield break;
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
}
