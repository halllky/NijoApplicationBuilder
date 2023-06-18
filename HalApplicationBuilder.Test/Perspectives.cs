using System;
namespace HalApplicationBuilder.Test.Tests {
    public partial class Perspectives : IClassFixture<SharedResource> {
        public Perspectives(SharedResource resoruce) {
            Project = resoruce.Project;
        }

        public HalappProject Project { get; }

        public static IEnumerable<object[]> Patterns() {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");
            var dir = Path.Combine(root, "DataPatterns");
            if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);
            foreach (var file in Directory.GetFiles(dir)) {
                // yield return new object[] { new DataPattern { XmlFilePath = file } };
            }

            yield return new object[] { new DataPattern { XmlFilePath = "/Users/halky/Documents/20230226_8_haldoc/HalApplicationBuilder.Test/DataPatterns/Childrenのみ.xml" } };
            yield return new object[] { new DataPattern { XmlFilePath = "/Users/halky/Documents/20230226_8_haldoc/HalApplicationBuilder.Test/DataPatterns/Refのみ.xml" } };
        }
    }
}
