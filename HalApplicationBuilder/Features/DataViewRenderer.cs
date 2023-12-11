using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using HalApplicationBuilder.Features.Searching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features {
    internal class DataViewRenderer {
        internal DataViewRenderer(GraphNode<DataView> dataView) {
            _dataView = dataView;
        }

        private readonly GraphNode<DataView> _dataView;

        internal string AppSrvMethodName => $"Search{_dataView.Item.DisplayName.ToCSharpSafe()}";

        internal MultiView2 GetMultiView() {
            var fields = _dataView
                .GetMembers()
                .OfType<AggregateMember.ValueMember>()
                .Select(m => new MultiViewField {
                    MemberType = m.Options.MemberType,
                    VisibleInGui = !m.Options.InvisibleInGui,
                    PhysicalName = m.GetFullPath().Join("_"),
                })
                .ToArray();
            return new MultiView2 {
                DisplayName = _dataView.Item.DisplayName,
                AppSrvMethodName = AppSrvMethodName,
                Fields = fields,
                CreateViewUrl = null,
                SingleViewUrlFunctionBody = null,
            };
        }

        internal SourceFile RenderCSharpCode(CodeRenderingContext ctx) {
            var controller = new WebClient.Controller(_dataView.Item.DisplayName.ToCSharpSafe());
            var multiView = GetMultiView();

            var sourceCode = $$"""
                {{controller.Render(ctx)}}

                {{multiView.RenderAspNetController(ctx)}}

                {{RenderAppServiceMethod(ctx)}}
                """;

            return new SourceFile {
                FileName = $"{_dataView.Item.DisplayName.ToFileNameSafe()}.cs",
                Content = sourceCode,
            };
        }

        private string RenderAppServiceMethod(CodeRenderingContext ctx) {
            var appSrv = new ApplicationService(ctx.Config);
            var multiView = GetMultiView();

            return $$"""
                namespace {{ctx.Config.RootNamespace}} {
                    partial class {{appSrv.ClassName}} {
                        public virtual IEnumerable<{{multiView.SearchResultClassName}}> {{AppSrvMethodName}}({{multiView.SearchConditionClassName}} conditions) {
                            // このメソッドは自動生成の対象外です。
                            // {{appSrv.ConcreteClass}}クラスでこのメソッドをオーバーライドして実装してください。
                            return Enumerable.Empty<{{multiView.SearchResultClassName}}>();
                        }
                    }
                }
                """;
        }
    }
}
