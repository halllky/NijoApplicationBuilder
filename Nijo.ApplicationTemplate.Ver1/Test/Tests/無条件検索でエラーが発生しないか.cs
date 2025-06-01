using MyApp.Core;
using MyApp.Core.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

partial class DB接続あり_更新なし {
    [TestCaseSource(typeof(QueryModelTestCases), nameof(QueryModelTestCases.無条件検索テストケース))]
    public async Task 無条件検索でエラーが発生しないか(string displayName, Func<ITestUtil, Task<IEnumerable<object>>> test) {
        // ダミーデータ投入
        using var util = TestUtilBuilder.Build();
        using var scope = util.CreateScope();

        var generator = new OverridedDummyDataGenerator();
        var dbDescriptor = new DummyDataDbOutput(scope.App.DbContext);
        await generator.GenerateAsync(dbDescriptor);

        // 無条件検索を実行
        Assert.DoesNotThrowAsync(async () => {
            var result = await test(util);
            Assert.That(result, Is.Not.Null);
            scope.App.Log.Debug("取得したデータ: {0}", scope.App.Configuration.ToJson(result));
        }, $"無条件検索でエラーが発生しました: {displayName}");
    }

    [TestCaseSource(typeof(QueryModelTestCases), nameof(QueryModelTestCases.無条件外部参照検索テストケース))]
    public async Task 無条件外部参照検索でエラーが発生しないか(string displayName, Func<ITestUtil, Task<IEnumerable<object>>> test) {
        // ダミーデータ投入
        using var util = TestUtilBuilder.Build();
        using var scope = util.CreateScope();

        var generator = new OverridedDummyDataGenerator();
        var dbDescriptor = new DummyDataDbOutput(scope.App.DbContext);
        await generator.GenerateAsync(dbDescriptor);

        // 無条件外部参照検索を実行
        Assert.DoesNotThrowAsync(async () => {
            var result = await test(util);
            Assert.That(result, Is.Not.Null);
            scope.App.Log.Debug("取得したデータ: {0}", scope.App.Configuration.ToJson(result));
        }, $"無条件外部参照検索でエラーが発生しました: {displayName}");
    }
}
