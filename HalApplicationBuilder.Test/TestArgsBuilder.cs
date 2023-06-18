using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Test {
    public class TestArgsBuilder : IEnumerable<object[]> {

        public IEnumerator<object[]> GetEnumerator() {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");
            var dir = Path.Combine(root, "DataPatterns");
            if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);
            foreach (var file in Directory.GetFiles(dir)) {
                yield return new object[] { new TestArgs(file) };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
