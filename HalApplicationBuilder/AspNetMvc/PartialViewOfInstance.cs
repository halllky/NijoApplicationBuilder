using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.AspNetMvc {
    partial class PartialViewOfInstanceTemplate {
        internal IEnumerable<KeyValuePair<string, object>> Members { get; set; }
    }
    internal class InstanceTemplateSchalarValueData {
        internal string AspForPath { get; set; }
    }
    internal class InstanceTemplateSchalarValueSearchConditionData {
        internal enum E_Type {
            Input,
            Range,
            Select,
        }
        internal string[] AspFor { get; set; }
        internal E_Type Type { get; set; }
        internal string EnumTypeName { get; set; }
        internal KeyValuePair<string, string>[] Options { get; set; }
    }
    internal class InstanceTemplateChildData {
        internal string ChildView { get; set; }
    }
    internal class InstanceTemplateChildrenData {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "<意図して小文字>")]
        internal string i { get; set; }
        internal string Count { get; set; }
        internal string PartialViewName { get; set; }
        internal string PartialViewBoundObjectName { get; set; }
        internal string AspForAddChild { get; set; }
        internal string AddButtonBoundObjectName { get; set; }

        internal static string AddButtonSenderIdentifier => JsTemplate.AGGREGATE_TREE_PATH_ATTR;
        internal static string AddButtonModelIdentifier => JsTemplate.AGGREGATE_MODEL_PATH_ATTR;
        internal static string AddButtonCssClass => JsTemplate.ADD_CHILD_BTN;
    }
    internal class InstanceTemplateReferencenData {
        internal string AspForKey { get; set; }
        internal string AspForText { get; set; }
    }
    internal class InstanceTemplateVariationData {
        internal string RadioButtonAspFor { get; set; }
        internal int Key { get; set; }
        internal string Name { get; set; }
        internal string ChildAggregateView { get; set; }
    }
    internal class InstanceTemplateVariationSearchConditionData {
        public string PropertyName { get; set; }
        public string AspFor { get; set; }
    }
}
