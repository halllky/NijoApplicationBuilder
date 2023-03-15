using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering
{
    internal class RenderedClass {
        internal required string CSharpTypeName { get; init; }
        internal required string ClassName { get; init; }
        internal required IEnumerable<RenderedProerty> Properties { get; init; }
    }

    internal class RenderedEFCoreEntity {
        internal required string CSharpTypeName { get; init; }
        internal required string ClassName { get; init; }
        internal required string DbSetName { get; init; }
        internal required IEnumerable<RenderedProerty> PrimaryKeys { get; init; }
        internal required IEnumerable<RenderedProerty> NonPrimaryKeys { get; init; }
        internal required IEnumerable<NavigationProerty> NavigationProperties { get; init; }
    }
}

