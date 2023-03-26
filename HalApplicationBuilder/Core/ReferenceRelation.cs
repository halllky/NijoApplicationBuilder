using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {
    /// <summary>
    /// 集約から別の集約への参照関係
    /// </summary>
    internal class ReferenceRelation {
        internal ReferenceRelation(MemberImpl.Reference source) {
            Source = source;
            Target = source.GetRefTarget();
        }

        internal MemberImpl.Reference Source { get; }
        internal Aggregate Target { get; }

        internal CodeRendering.RenderedClass GetEFCoreEntiyHavingOnlyReferredNavigationProp() {
            var refFrom = Source.Owner.ToDbEntity();
            var refTo = Target.ToDbEntity();
            return new CodeRendering.RenderedClass {
                ClassName = refTo.ClassName,
                CSharpTypeName = refTo.CSharpTypeName,
                Properties = new[] {
                    new CodeRendering.RenderedProperty {
                        Virtual = true,
                        CSharpTypeName = $"ICollection<{refFrom.CSharpTypeName}>",
                        PropertyName = $"{Source.GetCSharpSafeName()}_Refered",
                        Initializer = $"new HashSet<{refFrom.CSharpTypeName}>()",
                    },
                },
            };
        }
    }
}