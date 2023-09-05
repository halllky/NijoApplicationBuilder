using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.DotnetEx {
    internal static class ILoggerExtension {
        internal static ILogger CreateConsoleLogger() {
            return LoggerFactory.Create(builder => {

            }).CreateLogger<Program>();
        }
    }
}
