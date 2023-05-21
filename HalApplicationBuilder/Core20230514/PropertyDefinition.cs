using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core20230514 {
    public class PropertyDefinition {
        public bool Virtual { get; init; }
        public required string CSharpTypeName { get; init; }
        public required string PropertyName { get; init; }
        public string? Initializer { get; init; }
        public bool RequiredAtDB { get; init; }
    }
}
