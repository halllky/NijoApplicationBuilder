using Nijo.Core;
using Nijo.Models.WriteModel2Features;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// <see cref="DataClassForDisplay"/> を一括更新する処理。
    /// サーバー側で画面表示用データを <see cref="WriteModel2Features.DataClassForSave"/> に変換してForSaveの一括更新処理を呼ぶ。
    /// </summary>
    internal class BatchUpdateDisplayData : ISummarizedFile {

        private readonly List<GraphNode<Aggregate>> _aggregates = new();
        internal void Register(GraphNode<Aggregate> aggregate) {
            _aggregates.Add(aggregate);
        }

        // --------------------------------------------

        internal const string HOOK_NAME = "useBatchUpdateDisplayData";

        int ISummarizedFile.RenderingOrder => ((ISummarizedFile)new BatchUpdate()).RenderingOrder - 1; // BatchUpdateのソースに一部埋め込んでいるので
        void ISummarizedFile.OnEndGenerating(CodeRenderingContext context) {

            // 一括更新処理
            var batchUpdate = context.UseSummarizedFile<BatchUpdate>();
            batchUpdate.AddReactHook(HOOK_NAME, RenderReactHook(context));
            batchUpdate.AddControllerAction(RenderControllerAction(context));
            batchUpdate.AddAppSrvMethod(RenderAppSrvMethod(context));
        }

        internal string RenderReactHook(CodeRenderingContext context) {
            return $$"""
                // TODO #35
                """;
        }

        internal string RenderControllerAction(CodeRenderingContext context) {
            return $$"""
                // TODO #35
                """;
        }

        internal string RenderAppSrvMethod(CodeRenderingContext context) {
            return $$"""
                // TODO #35
                """;
        }
    }
}
