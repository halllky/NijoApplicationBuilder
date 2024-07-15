using Nijo.Core;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.ReadModel2Features {
    /// <summary>
    /// 一覧画面
    /// </summary>
    internal class MultiView : IReactPage {
        internal MultiView(GraphNode<Aggregate> agg) {
            _aggregate = agg;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        public string Url =>
             $$"""
                TODO #35
                """;

        public string DirNameInPageDir =>
             $$"""
                TODO #35
                """;

        public string ComponentPhysicalName =>
             $$"""
                TODO #35
                """;

        public bool ShowMenu => true;

        public string? LabelInMenu =>
             $$"""
                TODO #35
                """;

        public SourceFile GetSourceFile() => new SourceFile {
            FileName = "list.tsx",
            RenderContent = ctx => {
                return $$"""
                    TODO #35
                    """;
            },
        };
    }
}
