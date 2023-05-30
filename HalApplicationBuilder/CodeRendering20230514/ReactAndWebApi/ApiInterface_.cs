using HalApplicationBuilder.Core20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering20230514.ReactAndWebApi {
    partial class ApiInterface : ITemplate {

        internal ApiInterface(CodeRenderingContext ctx) {
            _ctx = ctx;
        }
        private readonly CodeRenderingContext _ctx;

        public string FileName => "Api.cs";
    }
}
