using System;
using System.Collections.Generic;

namespace haldoc.Models {
    public class CreateViewModel {
        public string PageTitle { get; set; }
        public Guid AggregateID { get; set; }
        public dynamic Instance { get; set; }
    }
}
