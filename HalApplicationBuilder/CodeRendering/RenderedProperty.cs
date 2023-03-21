using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.CodeRendering
{
    internal class RenderedProperty
    {
        internal bool Virtual { get; init; }
        internal required string CSharpTypeName { get; init; }
        internal required string PropertyName { get; init; }
        internal string? Initializer { get; init; }
    }

    internal class RenderedParentPkProperty : RenderedProperty {
    }
}

