using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Parts.WebClient {
    internal class MenuTsx {

        internal static SourceFile Render(CodeRenderingContext context) => new SourceFile {
            FileName = "autogenerated-menu.tsx",
            RenderContent = context => $$"""
                {{context.ReactProject.ReactPages.SelectTextTemplate(page => $$"""
                import {{page.ComponentPhysicalName}} from './{{ReactProject.PAGES}}/{{page.DirNameInPageDir}}/{{Path.GetFileNameWithoutExtension(page.GetSourceFile().FileName)}}'
                """)}}

                export const THIS_APPLICATION_NAME = '{{context.Schema.ApplicationName}}' as const

                export const routes: { url: string, el: JSX.Element }[] = [
                {{context.ReactProject.ReactPages.SelectTextTemplate(page => $$"""
                  { url: '{{page.Url}}', el: <{{page.ComponentPhysicalName}} /> },
                """)}}
                ]
                export const menuItems: { url: string, text: string }[] = [
                {{context.ReactProject.ReactPages.Where(p => p.ShowMenu).SelectTextTemplate(page => $$"""
                  { url: '{{page.Url}}', text: '{{page.LabelInMenu}}' },
                """)}}
                ]

                export const SHOW_LOCAL_REPOSITORY_MENU = {{(context.Config.DisableLocalRepository ? "false" : "true")}}
                """,
        };
    }
}
