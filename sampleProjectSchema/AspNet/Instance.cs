using System;
using System.Collections.Generic;
using System.Linq;
using haldoc.Core;
using haldoc.Core.Dto;

namespace haldoc.AspNet {
    public class Instance : haldoc.Core.Dto.IAutoGenerateClassMetadata {

        public Aggregate Aggregate { get; init; }
        public MvcContext MvcContext { get; init; }

        public string RuntimeClassFullName => Aggregate.Context.GetOutputNamespace(E_Namespace.DbEntity) + "." + RuntimeClassName;

        public string RuntimeClassName => Aggregate.Name + "__Instance";

        private List<PropertyTemplate> _properties;
        public IReadOnlyList<IAutoGeneratePropertyMetadata> Properties {
            get {
                if (_properties == null) {
                    _properties = Aggregate
                        .GetProperties()
                        .SelectMany(prop => prop.ToInstanceDtoProperty())
                        .ToList();
                }
                return _properties;
            }
        }
    }
}
