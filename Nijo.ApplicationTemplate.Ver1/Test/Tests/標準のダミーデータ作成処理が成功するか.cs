using MyApp.Core;
using MyApp.Core.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

partial class DB接続あり_更新あり {

    [Test]
    [Category("DB接続あり（更新あり）")]
    public void 標準のダミーデータ作成処理が成功するか() {
        using var util = TestUtilBuilder.Build();
        using var scope = util.CreateScope();

        var generator = new OverridedDummyDataGenerator();
        var dbDescriptor = new DummyDataDbOutput(scope.App.DbContext);

        Assert.DoesNotThrowAsync(async () => {
            try {
                await generator.GenerateAsync(dbDescriptor);

            } catch {
                // エラーが起きたデータのログ出力
                using var fs = File.OpenWrite(Path.Combine(scope.WorkDirectory, "作成しようとしたデータ.tsv"));
                using var sw = new StreamWriter(fs);
                var tsvDescriptor = new DummyDataTsvOutput(sw);
                await generator.GenerateAsync(tsvDescriptor);

                throw;
            }
        });
    }
}
