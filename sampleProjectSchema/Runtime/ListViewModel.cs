using System;
using System.Collections.Generic;
namespace haldoc.Runtime {
    public class ListViewModel<TSearchCondition, TListItem> {
        public TSearchCondition Filter { get; set; }
        public List<TListItem> Items { get; set; }
    }
}
