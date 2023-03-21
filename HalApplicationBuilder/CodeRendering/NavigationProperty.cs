using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.CodeRendering {

    internal class NavigationProperty : RenderedProperty {
        internal required OnModelCreatingDTO? OnModelCreating { get; init; }
    }

    internal class OnModelCreatingDTO {
        internal required string OpponentName { get; init; }
        internal required E_Multiplicity Multiplicity { get; init; }
        internal required IEnumerable<RenderedProperty> ForeignKeys { get; init; }
        internal required Microsoft.EntityFrameworkCore.DeleteBehavior OnDelete { get; init; }

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

