using System;
using Xunit;

namespace HalApplicationBuilder.Test.Tests.正常系 {
    public class Mvc経由の操作テスト {

        [Fact]
        public void 登録検索更新削除() {
            // DistMvcProject.Instance.GenerateCode(typeof(_20221210試用版.商品).Namespace);
            // DistMvcProject.Instance.BuildProject();
            using var web = DistMvcProject.WebProcess.Run();
            using var driver = web.GetFireFoxDriver();
            Assert.Equal("Home Page - HalApplicationBuilder.Test.DistMvc", driver.Title);
        }
    }
}
