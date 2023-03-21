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

    internal class NavigationProperty : RenderedProperty {
        internal required string OpponentName { get; init; }
        internal required bool IsPrincipal { get; init; }
        internal required E_Multiplicity Multiplicity { get; init; }
        internal required IEnumerable<RenderedProperty> ForeignKeys { get; init; }

        [Flags]
        internal enum E_Multiplicity {
            WithMany = 1,
            HasMany = 2,
            HasOneWithOne = 0,
            HasOneWithMany = 1,
            HasManyWithOne = 2,
            HasManyWithMany = 3,
        }
    }
}

