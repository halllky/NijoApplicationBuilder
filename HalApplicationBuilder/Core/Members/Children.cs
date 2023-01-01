﻿using System;
using System.Collections.Generic;

namespace HalApplicationBuilder.Core.Members {
    internal class Children : AggregateMemberBase {
        public override bool IsCollection => true;

        private Aggregate _child;
        private Aggregate ChildAggregat {
            get {
                if (_child == null) {
                    _child = new Aggregate {
                        Schema = Schema,
                        UnderlyingType = UnderlyingPropertyInfo.PropertyType.GetGenericArguments()[0],
                        Parent = this,
                    };
                }
                return _child;
            }
        }

        public override IEnumerable<Aggregate> GetChildAggregates() {
            yield return ChildAggregat;
        }

        internal override IEnumerable<AutoGenerateDbEntityProperty> ToDbColumnModel() {
            yield break;
        }

        internal override IEnumerable<AutoGenerateMvcModelProperty> ToInstanceModel(ViewRenderingContext context) {
            var propName = Name;
            var nested = context.Nest(propName, isCollection: true);
            var template = new ChildrenInstanceTemplate {
                i = context.LoopVar,
                Count = nested.CountPropertyPath,
                PartialViewName = new AspNetMvc.AggregatePartialView { Aggregate = ChildAggregat }.FileName,
                PartialViewBoundObjectName = nested.AspForPath,
            };
            yield return new AutoGenerateMvcModelProperty {
                CSharpTypeName = $"List<{ChildAggregat.ToInstanceModel(context).RuntimeFullName}>",
                PropertyName = propName,
                Initializer = "new()",
                View = template.TransformText(),
            };
        }

        internal override IEnumerable<AutoGenerateMvcModelProperty> ToSearchConditionModel(ViewRenderingContext context) {
            yield break;
        }

        internal override IEnumerable<AutoGenerateMvcModelProperty> ToSearchResultModel(ViewRenderingContext context) {
            yield break;
        }
    }

    partial class ChildrenInstanceTemplate {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "<意図して小文字>")]
        internal string i { get; set; }
        internal string Count { get; set; }
        internal string PartialViewName { get; set; }
        internal string PartialViewBoundObjectName { get; set; }
    }
}
