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
            public async Task Do(Func<Func<Task>, Task> scenario) {
                if (_describes.TryGetValue(_pattern.AsEnum(), out var describe)) {
                    await scenario(describe);
                } else {
                    Assert.Warn("期待結果が定義されていません。");
                }
            }
        }
        #endregion 期待結果が定義されていない場合にテストの事前準備をスキップするための仕組み
    }
}
