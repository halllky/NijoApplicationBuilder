using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.IntegrationTest.Perspectives {
    partial class Perspective {
        [UseDataPatterns]
        public async Task Createできるか(DataPattern pattern) {
            using var ct = new CancellationTokenSource();

            try {
                File.WriteAllText(SharedResource.Project.GetAggregateSchemaPath(), pattern.LoadXmlString());
                SharedResource.Project.Build();
                var task = SharedResource.Project.Run(ct.Token);

                switch (pattern.AsEnum()) {
                    case E_DataPattern._000_主キー1個の集約xml:
                        var res1 = await SharedResource.HttpPost("/api/集約A/create", new {
                            ID = "111",
                        });
                        Assert.That(res1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                        break;

                    default:
                        Assert.Warn("期待結果が未定義です。");
                        return;
                }

            } finally {
                ct.Cancel();
            }
        }
    }
}
