using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering
{
    internal class RenderedClass {
        internal required string CSharpTypeName { get; init; }
        internal required string ClassName { get; init; }
        internal required IEnumerable<RenderedProperty> Properties { get; init; }
    }

    internal class RenderedEFCoreEntity {
        internal required string CSharpTypeName { get; init; }
        internal required string ClassName { get; init; }
        internal required string DbSetName { get; init; }
        internal required IEnumerable<RenderedProperty> PrimaryKeys { get; init; }
        internal required IEnumerable<RenderedProperty> NonPrimaryKeys { get; init; }
        internal required IEnumerable<NavigationProperty> NavigationProperties { get; init; }
    }
}

