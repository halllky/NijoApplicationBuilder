using System;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.Models {
    public class ListViewModel {
        public string PageTitle { get; set; }
        public Guid AggregateID { get; set; }
        public IList<SearchConditionItem> SearchConditionItems { get; set; } = new List<SearchConditionItem>();
        public IList<string> TableHeader { get; set; } = new List<string>();
        public IList<IList<string>> SearchResults { get; set; } = new List<IList<string>>();
    }

    public class SearchConditionItem {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
