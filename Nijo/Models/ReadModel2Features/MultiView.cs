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

        public string Url => $"/{_aggregate.Item.UniqueId}"; // React Router は全角文字非対応なので
        public string DirNameInPageDir => _aggregate.Item.DisplayName.ToFileNameSafe();
        public string ComponentPhysicalName => $"{_aggregate.Item.PhysicalName}MultiView";
        public bool ShowMenu => true;
        public string? LabelInMenu => _aggregate.Item.DisplayName;

        public SourceFile GetSourceFile() => new SourceFile {
            FileName = "list.tsx",
            RenderContent = ctx => {
                return $$"""
                    export default function () {
                      return (
                        <div>
                          TODO #35 MultiView
                        </div>
                      )
                    }
                    """;
            },
        };
    }
}
