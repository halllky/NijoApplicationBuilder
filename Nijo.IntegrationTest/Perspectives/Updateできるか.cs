using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest.Perspectives {
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


            }).When(E_DataPattern._001_Refのみxml, async () => {
                var prepareSaki1 = await SharedResource.Project.Post("/api/参照先/create", new {
                    参照先集約ID = "111",
                    参照先集約名 = "参照先1",
                });
                var prepareSaki2 = await SharedResource.Project.Post("/api/参照先/create", new {
                    参照先集約ID = "222",
                    参照先集約名 = "参照先2",
                });
                var prepareMoto = await SharedResource.Project.Post("/api/参照元/create", new {
                    参照元集約ID = "333",
                    参照元集約名 = "参照元",
                    参照 = new { InstanceKey = "[\"111\"]", InstanceName = "/*なんでもいい*/" },
                });
                if (!prepareSaki1.IsSuccessStatusCode || !prepareSaki2.IsSuccessStatusCode || !prepareMoto.IsSuccessStatusCode) {
                    Assert.Warn("テスト前提条件の操作が失敗しています。");
                    return;
                }

                // 参照先の名称の変更
                var updateSaki1 = await SharedResource.Project.Post("/api/参照先/update/", new {
                    参照先集約ID = "111",
                    参照先集約名 = "参照先1（更新）",
                });
                var getMoto = await SharedResource.Project.Get("/api/参照元/detail/[\"333\"]");

                Util.AssertHttpResponseIsOK(getMoto);
                Assert.That(await getMoto.Content.ReadAsJsonAsync(), Is.EqualTo(new {
                    参照元集約ID = "333",
                    参照元集約名 = "参照元",
                    参照 = new { InstanceKey = "[\"111\"]", InstanceName = "参照先1（更新）" },
                    __halapp_InstanceKey = "[\"333\"]",
                    __halapp_InstanceName = "参照元",
                }.ToJson()));

                // 参照元の更新
                var updateMoto = await SharedResource.Project.Post("/api/参照元/update", new {
                    参照元集約ID = "333",
                    参照元集約名 = "参照元（更新）",
                    参照 = new { InstanceKey = "[\"222\"]", InstanceName = "/*なんでもいい*/" },
                });
                getMoto = await SharedResource.Project.Get("/api/参照元/detail/[\"333\"]");

                Util.AssertHttpResponseIsOK(getMoto);
                Assert.That(await getMoto.Content.ReadAsJsonAsync(), Is.EqualTo(new {
                    参照元集約ID = "333",
                    参照元集約名 = "参照元（更新）",
                    参照 = new { InstanceKey = "[\"222\"]", InstanceName = "参照先2" },
                    __halapp_InstanceKey = "[\"333\"]",
                    __halapp_InstanceName = "参照元（更新）",
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
                if (!res1.IsSuccessStatusCode) {
                    Assert.Warn("テスト前提条件の操作が失敗しています。");
                    return;
                }

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
                Util.AssertHttpResponseIsOK(res2);
                Util.AssertHttpResponseIsOK(res3);
                Assert.That(await res3.Content.ReadAsJsonAsync(), Is.EqualTo(new {
                    親集約ID = "111",
                    親集約名 = "親1（更新）",
                    子集約 = new[] {
                        new { 子集約ID = "222", 子集約名 = "子2（更新）" },
                        new { 子集約ID = "444", 子集約名 = "子4（追加）" },
                    },
                    __halapp_InstanceKey = "[\"111\"]",
                    __halapp_InstanceName = "親1（更新）",
                }.ToJson()));


            }).When(E_DataPattern._003_Childのみxml, async () => {
                var res1 = await SharedResource.Project.Post("/api/親集約/create", new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    子集約 = new {
                        子集約ID = "222",
                        子集約名 = "子2",
                    },
                });
                if (!res1.IsSuccessStatusCode) {
                    Assert.Warn("テスト前提条件の操作が失敗しています。");
                    return;
                }

                var res2 = await SharedResource.Project.Post("/api/親集約/update", new {
                    親集約ID = "111",
                    親集約名 = "親1（更新）",
                    子集約 = new {
                        子集約ID = "222",
                        子集約名 = "子2（更新）",
                    },
                });
                var res3 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");
                Util.AssertHttpResponseIsOK(res2);
                Util.AssertHttpResponseIsOK(res3);
                Assert.That(await res3.Content.ReadAsJsonAsync(), Is.EqualTo(new {
                    親集約ID = "111",
                    親集約名 = "親1（更新）",
                    子集約 = new {
                        子集約ID = "222",
                        子集約名 = "子2（更新）",
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
                if (!res1.IsSuccessStatusCode) {
                    Assert.Warn("テスト前提条件の操作が失敗しています。");
                    return;
                }

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
                Util.AssertHttpResponseIsOK(res2);
                Util.AssertHttpResponseIsOK(res3);
                Assert.That(await res3.Content.ReadAsJsonAsync(), Is.EqualTo(new {
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


            }).LaunchWebApi();
        }
    }
}
