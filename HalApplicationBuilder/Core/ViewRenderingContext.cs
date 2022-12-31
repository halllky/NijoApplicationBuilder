using System;
using System.Collections.Generic;
using System.Linq;

namespace HalApplicationBuilder.Core {

    internal class ViewRenderingContext {
        internal ViewRenderingContext(params string[] ancestors) {
            _ancestors = ancestors;
        }

        private readonly IReadOnlyList<string> _ancestors;

        /// <summary>
        /// ルートオブジェクトからのパス
        /// </summary>
        internal string Path => string.Join(".", _ancestors);
        /// <summary>
        /// ルートオブジェクトからのパス（asp-forにバインドするために先頭の"Model"を除外したもの）
        /// </summary>
        internal string AspForPath => _ancestors[0] == "Model"
            ? string.Join(".", _ancestors.Skip(1))
            : string.Join(".", _ancestors);

        /// <summary>
        /// 配列の添字。現在レンダリング中のオブジェクトの深さが1なら"i"、2なら"j"、…と以下延々と続く
        /// </summary>
        internal string LoopVar {
            get {
                // zまで使い切ることはないだろう
                if (_ancestors.Count > 18) throw new InvalidOperationException("ループ変数名を使い切りました。'z'より深いオブジェクトをレンダリングするにはプログラム改修が必要です。");
                return char.ConvertFromUtf32(_ancestors.Count + 104); // 深さ1のときループ変数名"i"
            }
        }

        /// <summary>
        /// 入れ子を1段深くする
        /// </summary>
        /// <param name="propertyName">このプロパティの子としてネストする</param>
        /// <param name="isCollection">コレクションなら添字つき</param>
        internal ViewRenderingContext Nest(string propertyName, bool isCollection = false) {
            var nested = isCollection ? $"{propertyName}[{LoopVar}]" : propertyName;
            return new ViewRenderingContext(_ancestors.Union(new[] { nested }).ToArray());
        }
    }

}
