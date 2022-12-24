using System;
using System.Collections.Generic;

namespace haldoc.Core.Dto {
    public class PropertyTemplate {
        public string CSharpTypeName { get; set; }
        public string PropertyName { get; set; }
    }
    public class PropertyLayoutTemplate {
        public string PropertyName { get; set; }
        public List<string> Layout { get; set; }
    }
}
