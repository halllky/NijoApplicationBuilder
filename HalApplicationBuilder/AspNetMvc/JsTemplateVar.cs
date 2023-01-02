using System;
namespace HalApplicationBuilder.AspNetMvc {
    partial class JsTemplate {
        /// <summary>_Layout.cshtml で指定するために使う</summary>
        public const string FILE_NAME = "__HalAppJs.cshtml";

        /// <summary>シングルビューのajaxの送信に使う</summary>
        internal const string FORM_ID = "halapp-single-view-form";
        /// <summary>シングルビューのajaxの戻り値からフッターボタンを除外するためのもの</summary>
        internal const string FORM_FOOTER_ID = "halapp-single-view-form-footer";

        /// <summary>画面内のどの集約の子要素にデータを追加するかを特定するための文字列(DOM属性用)</summary>
        internal const string AGGREGATE_TREE_PATH_ATTR = "halapp-aggregate-tree-path";
        /// <summary>画面内のどの集約の子要素にデータを追加するかを特定するための文字列(C#引数用)</summary>
        internal const string AGGREGATE_TREE_PATH_ARG = "aggregateTreePath";

        /// <summary>CSSクラス名</summary>
        internal const string ADD_CHILD_BTN = "halapp-add-child";
        internal static string ADD_CHILD_CTL => nameof(ControllerBase<object, object, object>.NewChild);
        /// <summary><see cref="ControllerBase{TSearchCondition, TSearchResult, TInstanceModel}.NewChild(string, int)"/> の引数名</summary>
        internal const string ADD_CHILD_ARG_2 = "currentArrayCount";
    }
}
