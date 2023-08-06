using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace HalApplicationBuilder.IntegrationTest.Perspectives {
    partial class Perspective {
        [UseDataPatterns]
        public async Task Createできるか(DataPattern pattern) {

            await If(pattern).When(E_DataPattern._000_主キー1個の集約xml, async () => {
                var res1 = await SharedResource.Project.Post("/api/集約A/create", new {
                    ID = "111",
                });
                var res2 = await SharedResource.Project.Get("/api/集約A/detail/[\"111\"]");

                Assert.That(res1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(await res2.Content.ReadAsJsonAsync(), Is.EqualTo(new {
                    ID = "111",
                    __halapp_InstanceKey = "[\"111\"]",
                    __halapp_InstanceName = "111",
                }.ToJson()));


            }).When(E_DataPattern._001_Refのみxml, async () => {
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

                Assert.That(createResult1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(createResult2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(findResult2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(await findResult2.Content.ReadAsJsonAsync(), Is.EqualTo(new {
                    参照元集約ID = "222",
                    参照元集約名 = "参照元2",
                    参照 = new { InstanceKey = "[\"111\"]", InstanceName = "参照先1" },
                    __halapp_InstanceKey = "[\"222\"]",
                    __halapp_InstanceName = "参照元2",
                }.ToJson()));


            }).When(E_DataPattern._002_Childrenのみxml, async () => {
                var createResult1 = await SharedResource.Project.Post("/api/親集約/create", new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    子集約 = new[] {
                        new { 子集約ID = "222", 子集約名 = "子2" },
                        new { 子集約ID = "333", 子集約名 = "子3" },
                    },
                });
                var findResult1 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");

                Assert.That(createResult1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(findResult1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(await findResult1.Content.ReadAsJsonAsync(), Is.EqualTo(new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    子集約 = new[] {
                        new { 子集約ID = "222", 子集約名 = "子2" },
                        new { 子集約ID = "333", 子集約名 = "子3" },
                    },
                    __halapp_InstanceKey = "[\"111\"]",
                    __halapp_InstanceName = "親1",
                }.ToJson()));


            }).When(E_DataPattern._003_Childのみxml, async () => {
                var createResult1 = await SharedResource.Project.Post("/api/親集約/create", new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    子集約 = new {
                        子集約ID = "222",
                        子集約名 = "子2",
                    },
                });
                var findResult1 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");

                Assert.That(createResult1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(findResult1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(await findResult1.Content.ReadAsJsonAsync(), Is.EqualTo(new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    子集約 = new {
                        子集約ID = "222",
                        子集約名 = "子2",
                    },
                    __halapp_InstanceKey = "[\"111\"]",
                    __halapp_InstanceName = "親1",
                }.ToJson()));


            }).When(E_DataPattern._004_Variationのみxml, async () => {
                var createResult1 = await SharedResource.Project.Post("/api/親集約/create", new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    種別 = 1,
                    種別_種別A = new {
                        種別Aのみに存在する属性 = "あああ",
                    },
                    種別_種別B = new {
                    },
                });
                var findResult1 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");

                Assert.That(createResult1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(findResult1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(await findResult1.Content.ReadAsJsonAsync(), Is.EqualTo(new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    種別 = 1,
                    種別_種別A = new {
                        種別Aのみに存在する属性 = "あああ",
                    },
                    種別_種別B = new {
                    },
                    __halapp_InstanceKey = "[\"111\"]",
                    __halapp_InstanceName = "親1",
                }.ToJson()));


            }).Do(async act => {
                using var ct = new CancellationTokenSource();
                using var dotnetRun = SharedResource.Project.CreateServerProcess(ct.Token);
                try {
                    SharedResource.Project.Build(pattern);
                    await dotnetRun.Launch();
                    await act();
                } finally {
                    ct.Cancel();
                }
            });
        }
    }
}
