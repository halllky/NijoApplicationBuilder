using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.IntegrationTest.Perspectives {
    partial class Perspective {

        [UseDataPatterns]
        public async Task Webから追加更新削除(DataPattern pattern) {

            await If(pattern).When(E_DataPattern._001_Refのみxml, async () => {
                using var driver = SharedResource.Project.CreateWebDriver();

                // トップページに移動
                var root = SharedResource.Project.Debugger.GetDebuggingClientUrl();
                driver.Navigate().GoToUrl(new Uri(root, "/0980ef37494eb0089d5695ded11e38fa"));

            }).LaunchWebApiAndClient();
        }
    }
}
