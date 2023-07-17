using HalApplicationBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.Presentation {
    partial class AggregateInstanceBase : ITemplate {
        internal AggregateInstanceBase(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "AggregateInstanceBase.cs";

        internal string Namespace => _ctx.Config.RootNamespace;
        internal string ClassFullname => $"{Namespace}.{CLASS_NAME}";
        internal static string CLASS_NAME => AggregateInstance.BASE_CLASS_NAME;
        internal const string INSTANCE_KEY = "__halapp_InstanceKey";
        internal const string INSTANCE_NAME = "__halapp_InstanceName";
    }
}
