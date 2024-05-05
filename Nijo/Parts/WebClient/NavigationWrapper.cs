using Nijo.Core;
using Nijo.Features.Storing;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    internal class NavigationWrapper {
        /// <summary>
        /// URLをラップする関数やフック
        /// </summary>
        internal NavigationWrapper(GraphNode<Aggregate> aggregate) {
            _aggregate = aggregate;
        }
        private readonly GraphNode<Aggregate> _aggregate;

        internal string GetSingleViewUrlHookName => $"get{_aggregate.Item.ClassName}SingleViewUrl";

        internal static SourceFile Render() => new SourceFile {
            FileName = "UrlUtil.ts",
            RenderContent = ctx => {
                var navigationHooks = ctx.Schema
                    .AllAggregates()
                    .Select(agg => new NavigationWrapper(agg));

                return $$"""
                    import { ItemKey } from './LocalRepository'

                    {{navigationHooks.SelectTextTemplate(nav => $$"""
                    {{nav.RenderHooks()}}

                    """)}}
                    """;
            },
        };

        private string RenderHooks() {
            var create = new SingleView(_aggregate.GetRoot(), SingleView.E_Type.Create);
            var view = new SingleView(_aggregate.GetRoot(), SingleView.E_Type.View);
            var edit = new SingleView(_aggregate.GetRoot(), SingleView.E_Type.Edit);
            var keyArray = KeyArray.Create(_aggregate);

            return $$"""
                export const {{GetSingleViewUrlHookName}} = (key: ItemKey | undefined, mode: 'new' | 'view' | 'edit'): string => {
                  if (!key) {
                    return ''
                  }
                  if (mode === 'new') {
                    return `{{create.GetUrlStringForReact(["key"])}}`
                  }
                  const [{{keyArray.Select(k => k.VarName).Join(", ")}}] = JSON.parse(key) as [{{keyArray.Select(k => $"{k.TsType} | undefined").Join(", ")}}]
                  if (mode === 'view') {
                    return `{{view.GetUrlStringForReact(keyArray.Select(k => k.VarName))}}`
                  } else {
                    return `{{edit.GetUrlStringForReact(keyArray.Select(k => k.VarName))}}`
                  }
                }
                """;
        }
    }
}
