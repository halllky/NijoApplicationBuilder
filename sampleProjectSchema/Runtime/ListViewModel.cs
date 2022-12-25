using System;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.Runtime {
    public class ListViewModel {
        public object Filter { get; set; }
        public List<object> Items { get; set; } = new();
    }
    public class ListViewModel<TSearchCondition, TListItem> : ListViewModel {
        public new TSearchCondition Filter {
            get => (TSearchCondition)base.Filter;
            set => base.Filter = value;
        }
        public new List<TListItem> Items {
            get => base.Items.Cast<TListItem>().ToList();
            set => base.Items = value?.Cast<object>().ToList();
        }
    }
}
