using System;
using HalApplicationBuilder.Core.UIModel;
using System.Collections.Generic;

namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering
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

