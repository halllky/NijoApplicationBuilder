using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HalApplicationBuilder.IntegrationTest.Perspectives {
    partial class Perspective {
        [UseDataPatterns]
        public async Task Createできるか(DataPattern pattern) {

            // プロジェクト起動
            using var ct = new CancellationTokenSource();
            Task? task = null;
            try {
                SharedResource.Project.Build(pattern);
                task = SharedResource.Project.Run(ct.Token);

                // 処理実行 & 結果確認
                switch (pattern.AsEnum()) {
                    case E_DataPattern._000_主キー1個の集約xml: {
                            var res1 = await SharedResource.Project.Post("/api/集約A/create", new {
                                ID = "111",
                            });
                            var res2 = await SharedResource.Project.Get("/api/集約A/detail/[\"111\"]");
                            var found = await res2.Content.ReadAsJsonAsync();

                            Assert.That(found, Is.EqualTo(new {
                                ID = "111",
                                __halapp_InstanceKey = "[\"111\"]",
                                __halapp_InstanceName = "111",
                            }.ToJson()));
                        }
                        break;

                    case E_DataPattern._001_Refのみxml: {
                            var createResult1 = await SharedResource.Project.Post("/api/参照先/create", new {
                                参照先集約ID = "111",
                                参照先集約名 = "参照先1",
                            });
                            var createResult2 = await SharedResource.Project.Post("/api/参照元/create", new {
                                参照元集約ID = "222",
                                参照元集約名 = "参照元2",
                                参照 = new { InstanceKey = "[\"111\"]", InstanceName = "/*なんでもいい*/" },
                            });
                            var findResult2 = await SharedResource.Project.Get("/api/参照元/detail/[\"222\"]");
                            var found = await findResult2.Content.ReadAsJsonAsync();

                            Assert.That(found, Is.EqualTo(new {
                                参照元集約ID = "222",
                                参照元集約名 = "参照元2",
                                照 = new { InstanceKey = "[\"111\"]", InstanceName = "参照先1" },
                            }.ToJson()));
                        }
                        break;

                    default:
                        Assert.Warn("期待結果が未定義です。");
                        return;
                }
            } finally {
                ct.Cancel();
                if (task != null) await task; // Webプロセスがkillされるのを待つ
            }
        }
    }
}
