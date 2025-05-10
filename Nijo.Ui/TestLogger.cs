using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ui;

/// <summary>
/// なんでもよいので適当に出力するログ機能
/// </summary>
internal class TestLogger : ILogger {
    IDisposable? ILogger.BeginScope<TState>(TState state) {
        Console.WriteLine(state);
        return null;
    }

    bool ILogger.IsEnabled(LogLevel logLevel) {
        return true;
    }

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        Console.WriteLine(formatter(state, exception));
    }
}
