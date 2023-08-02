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
