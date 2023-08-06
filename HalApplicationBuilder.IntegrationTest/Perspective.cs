using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.IntegrationTest.Perspectives {
    [NonParallelizable]
    public partial class Perspective {

        #region 期待結果が定義されていない場合にテストの事前準備をスキップするための仕組み
        private static DelayedExecuter If(DataPattern pattern) {
            return new DelayedExecuter(pattern);
        }
        private class DelayedExecuter {
            public DelayedExecuter(DataPattern pattern) {
                _pattern = pattern;
            }
            private readonly DataPattern _pattern;
            private readonly Dictionary<E_DataPattern, Func<Task>> _describes = new();
            public DelayedExecuter When(E_DataPattern pattern, Func<Task> then) {
                _describes[pattern] = then;
                return this;
            }
            public async Task LaunchTest() {
                if (!_describes.TryGetValue(_pattern.AsEnum(), out var describe)) {
                    Assert.Warn("期待結果が定義されていません。");
                    return;
                }

                using var ct = new CancellationTokenSource();
                using var dotnetRun = SharedResource.Project.CreateServerProcess(ct.Token);
                try {
                    SharedResource.Project.Build(_pattern);
                    await dotnetRun.Launch();
                    await describe();
                } finally {
                    ct.Cancel();
                }
            }
        }
        #endregion 期待結果が定義されていない場合にテストの事前準備をスキップするための仕組み
    }
}
