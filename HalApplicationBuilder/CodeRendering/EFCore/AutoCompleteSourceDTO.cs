using System;
namespace HalApplicationBuilder.CodeRendering.EFCore {

    internal class AutoCompleteSourceDTO {
        internal required string EntityClassName { get; init; }
        internal required string MethodName { get; init; }
        internal required string DbSetName { get; init; }
    }
}
