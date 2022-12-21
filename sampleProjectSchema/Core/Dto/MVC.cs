using System;
namespace haldoc.Core.Dto {
    public class TableHeader {
        public string Key { get; set; }
        public string Text { get; set; }
        public Aggregate LinkTo { get; set; }
    }
}
