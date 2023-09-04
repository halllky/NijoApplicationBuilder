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
                if (!res1.IsSuccessStatusCode) {
                    Assert.Warn("テスト前提条件の操作が失敗しています。");
                    return;
                }

                var res2 = await SharedResource.Project.Delete("/api/集約A/delete/[\"111\"]");
                var res3 = await SharedResource.Project.Get("/api/集約A/detail/[\"111\"]");
                Util.AssertHttpResponseIsOK(res2);
                Assert.That(res3.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));


            }).When(E_DataPattern._001_Refのみxml, async () => {
                var res1 = await SharedResource.Project.Post("/api/参照先/create", new {
                    参照先集約ID = "111",
                    参照先集約名 = "参照先1",
                });
                var res2 = await SharedResource.Project.Post("/api/参照元/create", new {
                    参照元集約ID = "222",
                    参照元集約名 = "参照元2",
                    参照 = new { InstanceKey = "[\"111\"]", InstanceName = "/*なんでもいい*/" },
                });
                if (!res1.IsSuccessStatusCode || !res2.IsSuccessStatusCode) {
                    Assert.Warn("テスト前提条件の操作が失敗しています。");
                    return;
                }

                // 参照元の削除
                var res3 = await SharedResource.Project.Delete("/api/参照元/delete/[\"222\"]");
                var res4 = await SharedResource.Project.Get("/api/参照先/detail/[\"111\"]");
                var res5 = await SharedResource.Project.Get("/api/参照元/detail/[\"222\"]");
                Util.AssertHttpResponseIsOK(res3);
                Util.AssertHttpResponseIsOK(res4);
                Assert.That(res5.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

                // 参照先の削除
                var res6 = await SharedResource.Project.Delete("/api/参照先/delete/[\"111\"]");
                var res7 = await SharedResource.Project.Get("/api/参照先/detail/[\"111\"]");
                Util.AssertHttpResponseIsOK(res6);
                Assert.That(res7.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));


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

                var res2 = await SharedResource.Project.Delete("/api/親集約/delete/[\"111\"]");
                var res3 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");
                Util.AssertHttpResponseIsOK(res2);
                Assert.That(res3.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                if (SharedResource.Project.ExecSql("SELECT * FROM 子集約 LIMIT 1").Any()) {
                    Assert.Fail("子要素テーブルにデータが残っている");
                }


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

                var res2 = await SharedResource.Project.Delete("/api/親集約/delete/[\"111\"]");
                var res3 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");
                Util.AssertHttpResponseIsOK(res2);
                Assert.That(res3.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                if (SharedResource.Project.ExecSql("SELECT * FROM 子集約 LIMIT 1").Any()) {
                    Assert.Fail("子要素テーブルにデータが残っている");
                }


            }).When(E_DataPattern._004_Variationのみxml, async () => {
                var res1 = await SharedResource.Project.Post("/api/親集約/create", new {
                    親集約ID = "111",
                    親集約名 = "親1",
                    種別 = 1,
                    種別_種別A = new {
                        種別Aのみに存在する属性 = "あああ",
                    },
                    種別_種別B = new {
                    },
                });
                if (!res1.IsSuccessStatusCode) {
                    Assert.Warn("テスト前提条件の操作が失敗しています。");
                    return;
                }

                var res2 = await SharedResource.Project.Delete("/api/親集約/delete/[\"111\"]");
                var res3 = await SharedResource.Project.Get("/api/親集約/detail/[\"111\"]");
                Util.AssertHttpResponseIsOK(res2);
                Assert.That(res3.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                if (SharedResource.Project.ExecSql("SELECT * FROM 種別A LIMIT 1").Any()) {
                    Assert.Fail("種別Aテーブルにデータが残っている");
                }
                if (SharedResource.Project.ExecSql("SELECT * FROM 種別B LIMIT 1").Any()) {
                    Assert.Fail("種別Bテーブルにデータが残っている");
                }


            }).LaunchWebApi();
        }
    }
}
