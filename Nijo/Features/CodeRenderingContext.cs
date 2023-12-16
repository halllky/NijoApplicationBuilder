using Nijo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features {
    internal class CodeRenderingContext {
        internal required Config Config { get; init; }
        internal required AppSchema Schema { get; init; }
    }
}
