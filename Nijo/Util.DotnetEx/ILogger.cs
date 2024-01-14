using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Util.DotnetEx {
    internal static class ILoggerExtension {
        internal static ILogger CreateConsoleLogger() {
            return LoggerFactory.Create(builder => {
                builder.AddSimpleConsole(opt => {
                    opt.SingleLine = true;
                    opt.IncludeScopes = true;
                });
            }).CreateLogger(string.Empty);
        }
    }
}
