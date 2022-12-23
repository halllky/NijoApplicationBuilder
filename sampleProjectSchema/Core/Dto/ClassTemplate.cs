using System;
using System.Collections.Generic;

namespace haldoc.Core.Dto {
    public class ClassTemplate {
        public string ClassName { get; set; }
        public List<PropertyTemplate> Properties { get; set; }
    }
}
