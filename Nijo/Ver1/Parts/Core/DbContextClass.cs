using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Parts.Core {
    public class DbContextClass : MultiAggregateSourceFileBase {
        public DbContextClass(CodeRenderingContext ctx) : base(ctx) {
        }

        public DbContextClass AddDbSet(string sourceCode) {
            throw new NotImplementedException();
        }
        public DbContextClass AddOnModelCreating(string sourceCode) {
            throw new NotImplementedException();
        }
        public DbContextClass AddConfigureConventions(string sourceCode) {
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
