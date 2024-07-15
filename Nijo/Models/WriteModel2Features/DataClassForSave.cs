using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.WriteModel2Features {
    /// <summary>
    /// 既存データの更新・削除のためのデータクラス
    /// </summary>
    internal class DataClassForSave {
        internal enum E_Type {
            Create,
            UpdateOrDelete,
        }

        internal DataClassForSave(GraphNode<Aggregate> agg, E_Type type) {
            _aggregate = agg;
            _type = type;
        }
        private readonly GraphNode<Aggregate> _aggregate;
        private readonly E_Type _type;

        /// <summary>
        /// このクラスの項目をEFCoreEntityにマッピングする処理をレンダリングします。
        /// </summary>
        private string RenderToDbEntity() {
            throw new NotImplementedException("TODO");
        }

        /// <summary>
        /// データ構造を定義します（C#）
        /// </summary>
        internal string RenderCSharp(CodeRenderingContext context) {
            throw new NotImplementedException("TODO");
        }
        /// <summary>
        /// データ構造を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScript(CodeRenderingContext context) {
            throw new NotImplementedException("TODO");
        }

        /// <summary>
        /// エラーメッセージ格納用の構造体を定義します（C#）
        /// </summary>
        internal string RenderCSharpReadOnlyStructure(CodeRenderingContext context) {
            throw new NotImplementedException("TODO");
        }
        /// <summary>
        /// エラーメッセージ格納用の構造体を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScriptErrorStructure(CodeRenderingContext context) {
            throw new NotImplementedException("TODO");
        }

        /// <summary>
        /// どの項目が読み取り専用かを表すための構造体を定義します（C#）
        /// </summary>
        internal string RenderTypeScriptReadOnlyStructure(CodeRenderingContext context) {
            throw new NotImplementedException("TODO");
        }
        /// <summary>
        /// どの項目が読み取り専用かを表すための構造体を定義します（TypeScript）
        /// </summary>
        internal string RenderCSharpErrorStructure(CodeRenderingContext context) {
            throw new NotImplementedException("TODO");
        }
    }
}
