using HalApplicationBuilder.CodeRendering20230514;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace HalApplicationBuilder.Test.Tests {
    public class ビルド {
        [Theory]
        [ClassData(typeof(TestArgsBuilder))]
        public void Test(TestArgs args) {
            var project = args.OpenProject();
            project.Build();
        }
    }
}
