using System.Collections.Generic;
using System.Reflection;

namespace HalApplicationBuilder.Runtime {
    public class MenuItem {
        public string LinkText { get; internal init; } = string.Empty;
        public string AspController { get; internal init; } = string.Empty;
    }
}
