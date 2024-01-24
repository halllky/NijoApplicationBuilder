using Microsoft.Extensions.Logging;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest {
    internal class TestContextLogger : ILogger {

        public bool IsEnabled(LogLevel logLevel) {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            var scope = string.Concat(_scope.Reverse().Select(x => $"{x} => "));
            TestContext.WriteLine($"{DateTime.Now:g}\t[{logLevel}]\t{scope}{formatter(state, exception)}");
        }

        #region スコープ
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
            _scope.Push(state.ToString() ?? string.Empty);
            return new Scope(this);
        }
        private class Scope(TestContextLogger owner) : IDisposable {
            private readonly TestContextLogger _owner = owner;
            private bool _disposed;

            public void Dispose() {
                if (_disposed) return;
                _owner._scope.Pop();
                _disposed = true;
            }
        }
        private readonly Stack<string> _scope = new();
        #endregion スコープ
    }
}
