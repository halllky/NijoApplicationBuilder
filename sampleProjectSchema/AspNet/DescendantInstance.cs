using System;
using System.Collections.Generic;
using haldoc.Core;
using haldoc.Core.Dto;

namespace haldoc.AspNet {
    public class DescendantInstance : Instance, haldoc.Core.Dto.IAutoGeneratePropertyMetadata {
        public AggregatePropBase PropBase { get; init; }

        public bool Virtual => false;
        public string CSharpTypeName => throw new NotImplementedException();
        public string RuntimePropertyName => throw new NotImplementedException();
        public string Initializer => throw new NotImplementedException();
    }
}
