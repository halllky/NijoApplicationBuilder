using Nijo.Core;
using Nijo.Parts.WebServer;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BatchUpdate {
    partial class BatchUpdateFeature {

        private static SourceFile RenderParamBuilder(CodeRenderingContext context) {
            var appSrv = new ApplicationService();
            var availableAggregates = GetAvailableAggregates(context).ToArray();

            return new SourceFile {
                FileName = "BatchUpdateTask_Helper.cs",
                RenderContent = () => $$"""
                    namespace {{context.Config.RootNamespace}} {
                        {{WithIndent(availableAggregates.SelectTextTemplate(RenderParamBuilder), "    ")}}
                    }
                    """,
            };
        }
        private static string RenderParamBuilder(GraphNode<Aggregate> agg) {
            var className = $"{agg.Item.ClassName}BatchUpdateParameter";
            var create = new Models.WriteModel.CreateFeature(agg);
            var update = new Models.WriteModel.UpdateFeature(agg);
            var delKeys = KeyArray.Create(agg);

            return $$"""

                /// <summary>
                /// <see cref="BatchUpdateParameter" /> に静的型がついていないのを補完して使いやすくするためのクラス
                /// </summary>
                public class {{className}} {
                    private readonly List<BatchUpdateData> _data = new();

                    public {{className}} Add({{create.ArgType}} cmd) {
                        _data.Add(new BatchUpdateData { Action = E_BatchUpdateAction.Add, Data = cmd });
                        return this;
                    }
                    public {{className}} Modify({{update.ArgType}} item) {
                        _data.Add(new BatchUpdateData { Action = E_BatchUpdateAction.Modify, Data = item });
                        return this;
                    }
                    public {{className}} Delete({{delKeys.Select(k => $"{k.CsType} {k.VarName}").Join(", ")}}) {
                        _data.Add(new BatchUpdateData { Action = E_BatchUpdateAction.Delete, Data = new object[] { {{delKeys.Select(k => k.VarName).Join(", ")}} } });
                        return this;
                    }
                    public BatchUpdateParameter Build() => new BatchUpdateParameter {
                        DataType = "{{GetKey(agg)}}",
                        Items = _data.ToList(),
                    };
                }
                """;
        }
    }
}
