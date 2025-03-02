using Nijo.Ver1.CodeGenerating;
using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.Models {
    internal class QueryModel : IModel {
        public void GenerateCode(CodeRenderingContext ctx, RootAggregate rootAggregate) {

            // データ型: 検索条件クラス
            // - CS
            // - TS
            //   - export type 検索条件型
            //   - export type ソート可能メンバー型
            // - TS側オブジェクト作成関数

            // 処理: URL変換
            // - URL => TS
            // - TS => URL

            // データ型: 画面表示用型 DisplayData

        }

        public void GenerateCode(CodeRenderingContext ctx) {

        }
    }
}
