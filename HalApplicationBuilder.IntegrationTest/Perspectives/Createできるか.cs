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
            // 期待結果の定義
            string create;
            string find;
            object data;
            switch (pattern.AsEnum()) {
                case E_DataPattern._000_主キー1個の集約xml:
                    create = "/api/集約A/create";
                    find = "/api/集約A/detail/[\"111\"]";
                    data = new {
                        ID = "111",
                    };
                    break;

                default:
                    Assert.Warn("期待結果が未定義です。");
                    return;
            }

            // プロジェクト起動して結果確認
            using var ct = new CancellationTokenSource();
            Task? task = null;
            try {
                SharedResource.Project.Build(pattern);
                task = SharedResource.Project.Run(ct.Token);

                var createResponse = await SharedResource.Project.Post(create, data);
                Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

                var findResponse = await SharedResource.Project.Get(find);
                Assert.That(findResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

                var found = await findResponse.Content.ReadAsJsonAsync();
                Assert.That(found, Is.EqualTo(data.ToJson()));

            } finally {
                ct.Cancel();
                if (task != null) await task; // Webプロセスがkillされるのを待つ
            }
        }
    }
}
