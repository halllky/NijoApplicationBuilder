using System;
using System.Collections.Generic;
using System.Linq;

namespace haldoc.Core.Dto {
    public class PropertyTemplate : IAutoGeneratePropertyMetadata {
        public string CSharpTypeName { get; set; }
        public string PropertyName { get; set; }
        public string Initializer { get; set; }
        string IAutoGeneratePropertyMetadata.RuntimePropertyName => PropertyName;
        public bool Virtual { get; set; }
    }
    public class PropertyLayoutTemplate {
        public string PropertyName { get; set; }
        public IEnumerable<string> Layout { get; set; }
    }
    public interface IAutoGeneratePropertyMetadata {
        bool Virtual { get; }
        string CSharpTypeName { get; }
        string RuntimePropertyName { get; }
        string Initializer { get; }
    }
}
