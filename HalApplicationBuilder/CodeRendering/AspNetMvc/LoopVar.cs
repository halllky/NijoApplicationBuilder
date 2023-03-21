using System;
namespace HalApplicationBuilder.CodeRendering.AspNetMvc {
    /// <summary>
    /// 配列の添字。現在レンダリング中のオブジェクトの深さが1なら"i"、2なら"j"、…となる
    /// </summary>
    internal class LoopVar {
        internal LoopVar(ObjectPath objectPath) {
            // zまで使い切ることはないだろう
            if (objectPath.CurrentDepth > 18) throw new InvalidOperationException("ループ変数名を使い切りました。'z'より深いオブジェクトをレンダリングするにはプログラム改修が必要です。");
            Value = char.ConvertFromUtf32(objectPath.CurrentDepth + 104); // 深さ1のときループ変数名"i"
        }

        internal string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }
}

