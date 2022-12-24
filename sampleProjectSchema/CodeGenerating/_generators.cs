using System;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.CodeGenerating {

    #region Model
    partial class MvcModelGenerator {
        public Core.ProjectContext Context { get; init; }
    }
    #endregion Model

    #region ListView
    partial class ListViewGenerator {
        public Core.Aggregate Aggregate { get; init; }
    }
    #endregion ListView

    partial class EFCodeGenerator {
        public Core.ProjectContext Context { get; init; }
    }
}
