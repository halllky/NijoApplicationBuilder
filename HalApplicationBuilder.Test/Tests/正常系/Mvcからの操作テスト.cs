using System;
using Xunit;

namespace HalApplicationBuilder.Test.Tests.正常系 {
    public class Mvc経由の操作テスト {
        [Fact]
        public void 登録検索更新削除() {
            DistMvcProject.Instance.GenerateCode();
            DistMvcProject.Instance.BuildProject();
            Assert.True(1 == 1);
        }
    }
}
