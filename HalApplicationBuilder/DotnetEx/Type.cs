using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {
    internal static class TypeExtensions {

        internal static string GetFullName(System.Type type) {
            if (type.IsGenericType) {
                string typeName = type.Name.Substring(0, type.Name.IndexOf('`'));
                string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFullName));
                return $"{type.Namespace}.{typeName}<{genericArgs}>";
            } else if (type.IsNested) {
                string outerName = GetFullName(type.DeclaringType!);
                string innerName = type.Name;
                return $"{outerName}.{innerName}";
            } else {
                return $"{type.Namespace}.{type.Name}";
            }
        }
    }
}
