using System;
using HalApplicationBuilder.Core.UIModel;
using System.Collections.Generic;

namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering
{
    internal class RenderedProerty
    {
        internal required bool Virtual { get; init; }
        internal required string CSharpTypeName { get; init; }
        internal required string PropertyName { get; init; }
        internal required string? Initializer { get; init; }
    }

    internal class NavigationProerty : RenderedProerty {
        internal required string OpponentName { get; init; }
        internal required bool IsPrincipal { get; init; }
        internal required bool IsManyToOne { get; init; }
    }
}

