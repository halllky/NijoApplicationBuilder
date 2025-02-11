using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest.DataPatternsClass {
    public class _017_CommandModel : DataPatternFromXml {
        public _017_CommandModel() : base("017_CommandModel.xml") { }

        protected override string OverridedAppSrvMethods() {
            return $$"""
                public override Task<ICommandResult> Execute従業員データ一括取り込み(従業員データ一括取り込みParameter param, ICommandResultGenerator<従業員データ一括取り込みParameterMessages> result) {
                    if (param.処理範囲 == E_処理範囲.特定データのみ) {
                        if (param.特定データのみ?.従業員?.内部ID == null) {
                            return Task.FromResult(result.Error("対象の従業員が指定されていません。"));
                        }
                        var searchResult = Load従業員(new() {
                            Filter = new() {
                                内部ID = param.特定データのみ.従業員.内部ID,
                            },
                            Skip = 0,
                            Take = 1,
                        }).ToArray();

                        if (searchResult.Length == 0) {
                            return Task.FromResult(result.Error("従業員データが見つかりません。"));
                        }

                        var displayData = searchResult[0];
                        displayData.Values.名前 += "あ"; // リダイレクト先の編集画面では、名前を書き換えた状態の初期値が表示される
                        return Task.FromResult(result.Redirect(displayData, E_SingleViewType.Edit, E_RefetchType.Overwrite));

                    } else {
                        return Task.FromResult(result.Ok("オッケーです"));

                        //// テキストファイルの内容を生成
                        //var content = "これはテキストファイルの内容です。\n行1\n行2\n行3";
                        //var bytes = Encoding.UTF8.GetBytes(content);

                        //// Fileメソッドを使用してファイルを返す
                        //return Task.FromResult(result.File(bytes, "text/plain"));
                    }
                }
                """;
        }
    }
}
