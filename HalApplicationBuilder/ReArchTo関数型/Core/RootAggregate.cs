using System;
using System.Collections.Generic;
using HalApplicationBuilder.ReArchTo関数型.CodeRendering;

namespace HalApplicationBuilder.ReArchTo関数型.Core
{
    internal class RootAggregate : Aggregate
    {
        internal static RootAggregate FromReflection(Type underlyingType) {
            return new RootAggregate(underlyingType);
        }

        private RootAggregate(Type underlyingType) : base(underlyingType, null, null) {
        }

        internal IEnumerable<Aggregate> GetDescendants() {
            throw new NotImplementedException();
        }

        internal CodeRendering.SearchMethodDTO BuildSearchMethod(Config config, string paramVarName, string queryVarName)
        {
            var dto = new CodeRendering.SearchMethodDTO {
                MethodName = $"Search_{_underlyingType.Name}",
                ParamVarName = paramVarName,
                QueryVarName = queryVarName,
                DbSetName = ToDbEntity(config).DbSetName,
                SearchConditionClassName = ToSearchConditionClass(config).CSharpTypeName,
                SearchResultClassName = ToSearchResultClass(config).CSharpTypeName,
            };
            foreach (var member in GetMembers()) {
                member.BuildSearchMethod(dto);
            }
            return dto;
        }

        internal void RenderSearchCondition(CodeRendering.RenderingContext context) {
            throw new NotImplementedException();
        }

        internal CodeRendering.RenderedClass ToController(Config config) {
            throw new NotImplementedException();
        }
    }
}

