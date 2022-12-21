using System;
using System.Collections.Generic;

namespace haldoc.Core.Dto {

    public class EntityDef {
        public string TableName { get; set; }
        public IList<EntityColumnDef> Keys { get; set; }
        public IList<EntityColumnDef> NonKeyProps { get; set; }
    }

    public class EntityColumnDef {
        public string CSharpTypeName { get; set; }
        public string ColumnName { get; set; }
    }
}
