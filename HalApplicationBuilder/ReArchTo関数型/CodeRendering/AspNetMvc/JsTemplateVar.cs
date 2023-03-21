using System;

namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering.AspNetMvc {
    using ControllerBase = Runtime.AspNetMvc.ControllerBase<Runtime.SearchConditionBase, Runtime.SearchResultBase, Runtime.UIInstanceBase>;

    partial class JsTemplate {
        /// <summary>_Layout.cshtml で指定するために使う</summary>
        public const string FILE_NAME = "__HalAppJs.cshtml";

        /// <summary>シングルビューのajaxの送信に使う</summary>
        internal const string FORM_ID = "halapp-single-view-form";
        /// <summary>シングルビューのajaxの戻り値からフッターボタンを除外するためのもの</summary>
        internal const string FORM_FOOTER_ID = "halapp-single-view-form-footer";

        /// <summary>部分ビューの1段階外側</summary>
        internal const string AGG_PARTIAL_CONTAINER = "halapp-aggregate-container";

        /// <summary>
        /// 非同期処理でどの集約が処理を担当するかをクライアントからサーバーに伝えるための文字列
        /// - 画面内のどの集約の子要素にデータを追加するか
        /// - autocomplete
        /// </summary>
        internal const string AGGREGATE_TREE_PATH_ATTR = "halapp-aggregate-tree-path";

        /// <summary>画面内のどの集約の子要素にデータを追加するかを特定するための文字列(C#引数用)</summary>
        internal const string AGGREGATE_TREE_PATH_ARG = "aggregateTreePath";
        /// <summary>新規子要素作成時にname属性の先頭に付す文字列の生成用(DOM属性用)</summary>
        internal const string AGGREGATE_MODEL_PATH_ATTR = "halapp-aggregate-model-path";
        /// <summary>新規子要素作成時にname属性の先頭に付す文字列の生成用(C#引数用)</summary>
        internal const string AGGREGATE_MODEL_PATH_ARG = "modelPath";

        /// <summary>CSSクラス名</summary>
        internal const string ADD_CHILD_BTN = "halapp-add-child";
        internal const string ADD_CHILD_CTL = nameof(ControllerBase.NewChild);
        /// <summary><see cref="ADD_CHILD_CTL"/> の引数</summary>
        internal const string ADD_CHILD_ARG_2 = "currentArrayCount";

        /// <summary>CSSクラス名</summary>
        internal const string REMOVE_BTN = "halapp-remove-button";

        internal const string REMOVE_HIDDEN_FIELD = "halapp-removed";

        /// <summary>autocompleteのinput(text)のCSSクラス名</summary>
        internal const string AUTOCOMPLETE_INPUT = "halapp-autocomplete";
        /// <summary>autocompleteのinput(hidden)のCSSクラス名</summary>
        internal const string AUTOCOMPLETE_VALUE = "halapp-autocomplete-value";
        internal const string AGGREGATE_GUID = "halapp-aggregate-guid";
        internal const string NAMEOF_AUTOCOMPLETE_ACTION = nameof(ControllerBase.Autocomplete);
    }
}
