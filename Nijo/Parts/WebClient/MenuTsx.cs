using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nijo.Util.CodeGenerating.CodeRenderingContext;

namespace Nijo.Parts.WebClient {
    internal class MenuTsx {

        internal static SourceFile Render(CodeRenderingContext context) => new SourceFile {
            FileName = "autogenerated-menu.tsx",
            RenderContent = () => $$"""
                {{context.ReactPages.SelectTextTemplate(page => $$"""
                import {{page.ComponentPhysicalName}} from './{{REACT_PAGE_DIR}}/{{page.DirNameInPageDir}}/{{Path.GetFileNameWithoutExtension(page.GetSourceFile().FileName)}}'
                """)}}

                export const THIS_APPLICATION_NAME = '{{context.Schema.ApplicationName}}' as const

                export const routes: { url: string, el: JSX.Element }[] = [
                {{context.ReactPages.SelectTextTemplate(page => $$"""
                  { url: '{{page.Url}}', el: <{{page.ComponentPhysicalName}} /> },
                """)}}
                ]
                export const menuItems: { url: string, text: string }[] = [
                {{context.ReactPages.Where(p => p.ShowMenu).SelectTextTemplate(page => $$"""
                  { url: '{{page.Url}}', text: '{{page.LabelInMenu}}' },
                """)}}
                ]
                """,
        };
    }
}