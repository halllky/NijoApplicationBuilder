using System;
using System.Collections.Generic;
using HalApplicationBuilder.CodeRendering;

namespace HalApplicationBuilder.Core
{
    internal class RootAggregate : Aggregate
    {
        internal RootAggregate(Config config, IAggregateDefine def) : base(config, def, null) {
        }

        internal IEnumerable<Aggregate> GetDescendantsAndSelf() {
            yield return this;
            foreach (var descendant in GetDescendants()) {
                yield return descendant;
            }
        }
        internal IEnumerable<Aggregate> GetDescendants() {
            foreach (var member in GetMembers()) {
                foreach (var child in member.GetChildAggregates()) {
                    yield return child;
                }
            }
        }

        internal System.Reflection.MethodInfo GetSearchMethod(System.Reflection.Assembly runtimeAssembly, Microsoft.EntityFrameworkCore.DbContext dbContext) {
            var dbContextType = dbContext.GetType();
            var method = dbContextType.GetMethod(GetSearchMethodName());
            if (method == null) throw new InvalidOperationException($"{dbContextType.Name} にメソッド {GetSearchMethodName()} が存在しません。");
            return method;
        }
        internal CodeRendering.SearchMethodDTO BuildSearchMethod(string paramVarName, string queryVarName, string lambdaVarName) {
            var dto = new CodeRendering.SearchMethodDTO {
                MethodName = GetSearchMethodName(),
                ParamVarName = paramVarName,
                QueryVarName = queryVarName,
                SelectLambdaVarName = lambdaVarName,
                DbSetName = ToDbEntity().DbSetName,
                SearchConditionClassName = ToSearchConditionClass().CSharpTypeName,
                SearchResultClassName = ToSearchResultClass().CSharpTypeName,
            };
            foreach (var member in GetMembers()) {
                member.BuildSearchMethod(dto);
            }
            return dto;
        }
        private string GetSearchMethodName() => $"Search_{Name}";
    }
}

