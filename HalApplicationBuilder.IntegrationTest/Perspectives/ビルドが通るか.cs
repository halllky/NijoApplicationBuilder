using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.IntegrationTest.Perspectives {
    partial class Perspective {
        [UseDataPatterns]
        public void ビルドが通るか(DataPattern pattern) {
            File.WriteAllText(SharedResource.Project.GetAggregateSchemaPath(), pattern.LoadXmlString());
            SharedResource.Project.Build();
        }
    }
}
