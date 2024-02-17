using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx {
    internal static class ILoggerExtension {
        internal static ILogger CreateConsoleLogger(bool verbose = true) {
            return LoggerFactory.Create(builder => {

                builder.AddSimpleConsole(opt => {
                    opt.SingleLine = true;
                    opt.IncludeScopes = true;
                });

                if (verbose) {
                    builder.SetMinimumLevel(LogLevel.Trace);
                }
            }).CreateLogger(string.Empty);
        }
    }
}
