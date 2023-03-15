using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.ReArchTo関数型.Runtime.AspNetMvc {
    public class MultiViewModel<TSearchCondition, TSearchResult> {
        public required TSearchCondition SearchCondition { get; set; }
        public required List<TSearchResult> SearchResult { get; set; } = new();
    }
}

