using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nijo.Features.InstanceHandling;
using Nijo.Core;
using Nijo.DotnetEx;

namespace Nijo.Features.WebClient {
#pragma warning disable IDE1006 // 命名スタイル
    internal class index {
#pragma warning restore IDE1006 // 命名スタイル

        internal static SourceFile Render(IEnumerable<Infrastucture.IReactPage> reactPages) => new SourceFile {
            FileName = "index.tsx",
            RenderContent = ctx => $$"""
                import './nijo.css';
                import 'ag-grid-community/styles/ag-grid.css';
                import 'ag-grid-community/styles/ag-theme-alpine.css';

                {{reactPages.SelectTextTemplate(page => $$"""
                import {{page.ComponentPhysicalName}} from './{{Infrastucture.REACT_PAGE_DIR}}/{{page.DirNameInPageDir}}/{{Path.GetFileNameWithoutExtension(page.GetSourceFile().FileName)}}'
                """)}}

                export const THIS_APPLICATION_NAME = '{{ctx.Schema.ApplicationName}}' as const

                export const routes: { url: string, el: JSX.Element }[] = [
                {{reactPages.SelectTextTemplate(page => $$"""
                  { url: '{{page.Url}}', el: <{{page.ComponentPhysicalName}} /> },
                """)}}
                ]
                export const menuItems: { url: string, text: string }[] = [
                {{reactPages.Where(p => p.ShowMenu).SelectTextTemplate(page => $$"""
                  { url: '{{page.Url}}', text: '{{page.LabelInMenu}}' },
                """)}}
                ]
                """,
        };

        public class ImportedComponent {
            public required bool ShowMenu { get; init; }
            public required string Url { get; init; }
            public required string PhysicalName { get; init; }
            public required string DisplayName { get; init; }
            public required string From { get; init; }
        }
    }
}
