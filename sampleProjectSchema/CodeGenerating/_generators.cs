using System;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.CodeGenerating {

    #region Model
    partial class MvcModelGenerator {
        public Core.ProjectContext Context { get; init; }
    }
    public class ListViewModelTemplate {

    }
    #endregion Model

    #region ListView
    partial class ListViewGenerator {
        public Core.Aggregate Aggregate { get; init; }

        public IEnumerable<ListViewPropDef> GetListViewProps() {
            foreach (var prop in Aggregate.GetProperties()) {
                throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }
    }
    public class ListViewPropDef {
        public string Key { get; set; }
        public string Name { get; set; }

    }
    #endregion ListView

    partial class EFCodeGenerator {
        public Core.ProjectContext Context { get; init; }
    }
}
