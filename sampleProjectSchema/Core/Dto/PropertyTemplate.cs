using System;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.Core.Dto {
    public class PropertyTemplate {
        public string CSharpTypeName { get; set; }
        public string PropertyName { get; set; }
        public string Initializer { get; set; }
    }
    public class PropertyLayoutTemplate {
        public string PropertyName { get; set; }
        public IEnumerable<string> Layout { get; set; }
    }
}
