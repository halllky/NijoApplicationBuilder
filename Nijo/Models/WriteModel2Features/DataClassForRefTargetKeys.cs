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
    /// ほかの集約から参照されるときのためのデータクラス。
    /// 登録更新に必要なキー情報のみが定義される。
    /// </summary>
    internal class DataClassForRefTargetKeys {
        internal DataClassForRefTargetKeys(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }

        private readonly GraphNode<Aggregate> _aggregate;

        internal string CsClassName => $"/* TODO #35 DataClassForRefTargetKeys */";
        internal string TsTypeName => $"/* TODO #35 DataClassForRefTargetKeys */";

        /// <summary>
        /// データ構造を定義します（C#）
        /// </summary>
        internal string RenderCSharp(CodeRenderingContext context) {
            return $$"""
                // TODO #35 DataClassForRefTargetKeys
                """;
        }
        /// <summary>
        /// データ構造を定義します（TypeScript）
        /// </summary>
        internal string RenderTypeScript(CodeRenderingContext context) {
            return $$"""
                // TODO #35 DataClassForRefTargetKeys
                """;
        }

        /// <summary>
        /// 画面表示用データクラスを登録更新用データクラスに変換します。
        /// </summary>
        private string RenderFromDisplayData() {
            return $$"""
                // TODO #35 DataClassForRefTargetKeys RenderFromDisplayData
                """;
        }

        /// <summary>
        /// 登録更新用データクラスの値をEFCoreEntityにマッピングします。
        /// </summary>
        private string ToDbEntity() {
            return $$"""
                // TODO #35 DataClassForRefTargetKeys ToDbEntity
                """;
        }
    }
}
