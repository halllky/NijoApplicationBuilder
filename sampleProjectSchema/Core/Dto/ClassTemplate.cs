using System;
using System.Collections.Generic;

namespace haldoc.Core.Dto {
    public class ClassTemplate : IAutoGenerateClassMetadata {
        public string ClassName { get; set; }
        public List<PropertyTemplate> Properties { get; set; }

        public string RuntimeClassFullName => throw new NotImplementedException();

        string IAutoGenerateClassMetadata.RuntimeClassName => ClassName;
        IReadOnlyList<IAutoGeneratePropertyMetadata> IAutoGenerateClassMetadata.Properties => Properties;
    }
    public interface IAutoGenerateClassMetadata {
        string RuntimeClassFullName { get; }
        string RuntimeClassName { get; }
        IReadOnlyList<IAutoGeneratePropertyMetadata> Properties { get; }
    }
}
