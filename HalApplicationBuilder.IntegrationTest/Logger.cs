using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.IntegrationTest {
    internal class Logger : ILogger {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
            return null;
        }
        public bool IsEnabled(LogLevel logLevel) {
            return true;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            TestContext.Out.WriteLine($"{DateTime.Now:g}\t[{logLevel}]\t{formatter(state, exception)}");
        }
    }
}
