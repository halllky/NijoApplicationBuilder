using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.CodeRendering.EFCore {

    internal class NavigationProperty : RenderedProperty {
        internal required OnModelCreatingDTO? OnModelCreating { get; init; }
    }
}

