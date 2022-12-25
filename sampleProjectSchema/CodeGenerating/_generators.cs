using System;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.CodeGenerating {

    partial class EFCodeGenerator {
        public Core.ProjectContext Context { get; init; }
    }
    partial class MvcModelGenerator {
        public Core.ProjectContext Context { get; init; }
    }
    partial class ListViewGenerator {
        public Core.ProjectContext Context { get; init; }
        public Core.Aggregate Aggregate { get; init; }
    }
    partial class CreateViewGenerator {
        public Core.ProjectContext Context { get; init; }
        public Core.Aggregate Aggregate { get; init; }
    }
    partial class MvcControllerGenerator {
        public Core.ProjectContext Context { get; init; }
        public Core.Aggregate Aggregate { get; init; }
    }
}
