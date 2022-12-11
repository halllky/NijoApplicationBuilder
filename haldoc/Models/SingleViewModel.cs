using System;
using System.Collections.Generic;

namespace haldoc.Models {
    public class SingleViewModel {
        public string PageTitle { get; set; }
        public Guid AggregateID { get; set; }
        public dynamic Instance { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
