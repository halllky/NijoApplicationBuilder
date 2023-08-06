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
        public async Task Deleteできるか(DataPattern pattern) {

            await If(pattern).When(E_DataPattern._000_主キー1個の集約xml, async () => {
                var res1 = await SharedResource.Project.Post("/api/集約A/create", new {
                    ID = "111",
                });
                var res2 = await SharedResource.Project.Delete("/api/集約A/delete/[\"111\"]");
                var res3 = await SharedResource.Project.Get("/api/集約A/detail/[\"111\"]");

                Assert.That(res1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res3.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));


            }).When(E_DataPattern._002_Childrenのみxml, async () => {
                var res1 = await SharedResource.Project.Post("/api/親集約/create", new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    子集約 = new[] {
                        new { 子集約ID = "222", 子集約名 = "子2" },
                        new { 子集約ID = "333", 子集約名 = "子3" },
                    },
                });
                var res2 = await SharedResource.Project.Delete("/api/親集約/delete/[\"111\"]");
                var res3 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");

                Assert.That(res1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res3.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

                // 子要素データが削除されているか確認
                if (SharedResource.Project.ExecSql("SELECT * FROM 子集約 LIMIT 1").Any()) {
                    Assert.Fail("子要素テーブルにデータが残っている");
                }


            }).Do(async act => {
                using var ct = new CancellationTokenSource();
                Task? task = null;
                try {
                    SharedResource.Project.Build(pattern);
                    task = SharedResource.Project.Run(ct.Token);
                    await act();
                } finally {
                    ct.Cancel();
                    if (task != null) await task; // Webプロセスがkillされるのを待つ
                }
            });
        }
    }
}
