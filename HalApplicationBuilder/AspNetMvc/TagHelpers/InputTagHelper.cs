using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HalApplicationBuilder.AspNetMvc.TagHelpers {
    /// <summary>
    /// 子要素追加ボタンクリック時、部分ビューにHtmlFieldPrefixをつけるため、
    /// 追加ボタンに画面初期表示時のHtmlFieldPrefixを保持させるためのタグヘルパー。
    /// 
    /// input の asp-for の実装を参考にしている。
    /// https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.TagHelpers/src/InputTagHelper.cs
    /// </summary>
    [HtmlTargetElement("input", Attributes = JsTemplate.AGGREGATE_MODEL_PATH_ATTR, TagStructure = TagStructure.WithoutEndTag)]
    public class InputTagHelper : TagHelper {

        [HtmlAttributeName(JsTemplate.AGGREGATE_MODEL_PATH_ATTR)]
        public ModelExpression For { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output) {
            var fullName = $"{ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix}.{For.Name}";
            output.Attributes.Add(JsTemplate.AGGREGATE_MODEL_PATH_ATTR, fullName);
        }
    }
}
