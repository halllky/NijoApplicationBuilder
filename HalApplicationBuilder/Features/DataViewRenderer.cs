using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features {
    internal class DataViewRenderer : TemplateBase {
        internal DataViewRenderer(GraphNode<DataView> dataView, CodeRenderingContext ctx) {
            _dataView = dataView;
            _ctx = ctx;
        }

        private readonly GraphNode<DataView> _dataView;
        private readonly CodeRenderingContext _ctx;

        public override string FileName => $"{_dataView.Item.DisplayName.ToFileNameSafe()}.cs";
        internal string AppSrvMethodName => $"Search{_dataView.Item.DisplayName.ToCSharpSafe()}";

        protected override string Template() {
            var controller = new WebClient.Controller(_dataView.Item);
            var search = new Searching.SearchFeature(_dataView.As<IEFCoreEntity>(), _ctx);

            return $$"""
                {{controller.Render(_ctx)}}

                {{search.RenderControllerAction()}}

                {{RenderAppServiceMethod()}}
                """;
        }

        private string RenderAppServiceMethod() {
            var appSrv = new ApplicationService(_ctx.Config);
            var search = new Searching.SearchFeature(_dataView.As<IEFCoreEntity>(), _ctx);

            return $$"""
                namespace {{_ctx.Config.RootNamespace}} {
                    partial class {{appSrv.ClassName}} {
                        public virtual IEnumerable<{{search.SearchResultClassName}}> {{AppSrvMethodName}}({{search.SearchConditionClassName}} conditions) {
                            // このメソッドは自動生成の対象外です。
                            // {{appSrv.ConcreteClass}}クラスでこのメソッドをオーバーライドして実装してください。
                            return Enumerable.Empty<{{search.SearchResultClassName}}>();
                        }
                    }
                }
                """;
        }

        internal string RenderTypeScriptDefinition() {

        }
    }
}
