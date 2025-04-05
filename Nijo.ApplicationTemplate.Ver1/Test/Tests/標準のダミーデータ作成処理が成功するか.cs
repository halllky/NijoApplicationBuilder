using MyApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp;

partial class DB等入出力あり {

    [Test]
    public void 標準のダミーデータ作成処理が成功するか() {
        using var util = TestUtilBuilder.Build();
        using var scope = util.CreateScope();

        var generator = new DummyDataGenerator();
        var descriptor = new BasicDummyDataOutput(scope.App.DbContext);

        Assert.DoesNotThrowAsync(() => generator.GenerateAsync(descriptor));
    }
}
