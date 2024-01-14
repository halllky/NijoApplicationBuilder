using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest.Perspectives {
    public class _デバッグ用ビルド {
        [Test]
        [Ignore("動作確認が完了したため")]
        public async Task npm_startの終了後にプロセスが残り続けないか() {
            using var ct = new CancellationTokenSource();
            var terminal = new Terminal(SharedResource.Project.WebClientProjectRoot, new TestContextLogger());
            Task npmProcess;
            try {
                npmProcess = await terminal.RunBackground(
                    new[] { "npm", "run", "dev" },
                    new Regex("➜"),
                    Encoding.UTF8,
                    ct.Token);

                await Task.Delay(1000, ct.Token);

            } finally {
                ct.Cancel();
            }

            await npmProcess;
        }
    }
}
