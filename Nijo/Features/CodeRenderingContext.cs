using Nijo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features {
    public class CodeRenderingContext {
        public required Config Config { get; init; }
        public required AppSchema Schema { get; init; }
    }
}
