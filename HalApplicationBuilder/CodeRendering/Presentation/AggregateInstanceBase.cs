using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Presentation {
    partial class AggregateInstanceBase : TemplateBase {
        internal AggregateInstanceBase(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public override string FileName => "AggregateInstanceBase.cs";

        internal string Namespace => _ctx.Config.RootNamespace;
        internal string ClassFullname => $"{Namespace}.{CLASS_NAME}";
        internal static string CLASS_NAME => AggregateMember.BASE_CLASS_NAME;
        internal const string INSTANCE_KEY = "__halapp_InstanceKey";
        internal const string INSTANCE_NAME = "__halapp_InstanceName";

        protected override string Template() {
            return $$"""
                #pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
                #pragma warning disable IDE1006 // 命名スタイル

                namespace {{Namespace}} {
                    public abstract class {{CLASS_NAME}} {
                        public string {{INSTANCE_KEY}} { get; set; } = string.Empty;
                        public string {{INSTANCE_NAME}} { get; set; } = string.Empty;
                    }
                }
                """;
        }
    }
}
