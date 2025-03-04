using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models.DataModelModules {
    internal class DummyDataGenerator : MultiAggregateSourceFileBase {
        public DummyDataGenerator(CodeRenderingContext ctx) : base(ctx) {
        }

        internal void Add(RootAggregate rootAggregate) {
            throw new NotImplementedException();
        }

        internal override IEnumerable<object?> EnumerateState() {
            throw new NotImplementedException();
        }

        private protected override void Render() {
            throw new NotImplementedException();
        }
    }
}
