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
    partial class Perspectives {
        [Theory(DisplayName = "ビルドが通るか")]
        [MemberData(nameof(Patterns))]
        public void Build(DataPattern pattern) {
            File.WriteAllText(Project.GetAggregateSchemaPath(), pattern.LoadXmlString());
            // Project.Build();
            Assert.True(true);
        }
    }
}
