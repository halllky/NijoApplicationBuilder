using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core {
    internal class ReferredAggregate : Aggregate
    {
        internal ReferredAggregate(Config config, Type underlyingType, MemberImpl.Reference referredBy) : base(config, underlyingType, null)
        {
            _referredBy = referredBy;
        }

        private readonly MemberImpl.Reference _referredBy;

        internal CodeRendering.RenderedClass GetEFCoreEntiyHavingOnlyReferredNavigationProp() {
            var refFrom = _referredBy.Owner.ToDbEntity();
            var refTo = ToDbEntity();
            return new CodeRendering.RenderedClass {
                ClassName = refTo.ClassName,
                CSharpTypeName = refTo.CSharpTypeName,
                Properties = new[] {
                    new CodeRendering.RenderedProperty {
                        Virtual = true,
                        CSharpTypeName = $"ICollection<{refFrom.CSharpTypeName}>",
                        PropertyName = $"{_referredBy.Owner.Name}_Refered",
                        Initializer = $"new HashSet<{refFrom.CSharpTypeName}>()",
                    },
                },
            };
        }
    }
} 