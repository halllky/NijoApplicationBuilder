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
        public async Task Updateできるか(DataPattern pattern) {

            await If(pattern).When(E_DataPattern._000_主キー1個の集約xml, async () => {
                var res1 = await SharedResource.Project.Post("/api/集約A/create", new {
                    ID = "111",
                });
                var res2 = await SharedResource.Project.Post("/api/集約A/update", new {
                    ID = "111",
                });
                var res3 = await SharedResource.Project.Get("/api/集約A/detail/[\"111\"]");
                var found = await res2.Content.ReadAsJsonAsync();

                Assert.That(found, Is.EqualTo(new {
                    ID = "111",
                    __halapp_InstanceKey = "[\"111\"]",
                    __halapp_InstanceName = "111",
                }.ToJson()));


            }).When(E_DataPattern._002_Childrenのみxml, async () => {
                var res1 = await SharedResource.Project.Post("/api/親集約/create", new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    子集約 = new[] {
                        new { 子集約ID = "222", 子集約名 = "子2" },
                        new { 子集約ID = "333", 子集約名 = "子3" },
                    },
                });
                var res2 = await SharedResource.Project.Post("/api/親集約/update", new {
                    親集約ID = "111",
                    親集約名 = "親1（更新）",
                    子集約 = new[] {
                        new { 子集約ID = "222", 子集約名 = "子2（更新）" },
                        //new { 子集約ID = "333", 子集約名 = "子3（削除）" },
                        new { 子集約ID = "444", 子集約名 = "子4（追加）" },
                    },
                });
                var res3 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");
                var res3json = await res3.Content.ReadAsJsonAsync();

                Assert.That(res1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res3.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res3json, Is.EqualTo(new {
                    親集約ID = "111",
                    親集約名 = "親1（更新）",
                    子集約 = new[] {
                        new { 子集約ID = "222", 子集約名 = "子2（更新）" },
                        new { 子集約ID = "444", 子集約名 = "子4（追加）" },
                    },
                    __halapp_InstanceKey = "[\"111\"]",
                    __halapp_InstanceName = "親1（更新）",
                }.ToJson()));


            }).When(E_DataPattern._004_Variationのみxml, async () => {
                var res1 = await SharedResource.Project.Post("/api/親集約/create", new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    種別 = 1,
                    種別_種別A = new {
                        種別Aのみに存在する属性 = "種別A詳細",
                    },
                    種別_種別B = new {
                    },
                });
                var res2 = await SharedResource.Project.Post("/api/親集約/update", new {
                    親集約ID = "111",
                    親集約名 = "親1（更新後）",
                    種別 = 2,
                    種別_種別A = new {
                        種別Aのみに存在する属性 = "種別A詳細（更新後）",
                    },
                    種別_種別B = new {
                    },
                });
                var res3 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");
                var res3json = await res3.Content.ReadAsJsonAsync();

                Assert.That(res1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res3.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res3json, Is.EqualTo(new {
                    親集約ID = "111",
                    親集約名 = "親1（更新後）",
                    種別 = 2,
                    種別_種別A = new {
                        種別Aのみに存在する属性 = "種別A詳細（更新後）",
                    },
                    種別_種別B = new {
                    },
                    __halapp_InstanceKey = "[\"111\"]",
                    __halapp_InstanceName = "親1（更新後）",
                }.ToJson()));


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
