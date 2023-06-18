using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.IntegrationTest {
    public class DataPattern {
        public DataPattern(string path) {
            _xmlFilePath = path;
        }

        private readonly string _xmlFilePath;

        public string GetXmlFileName() {
            return Path.GetFileName(_xmlFilePath);
        }
        public string LoadXmlString() {
            return File.ReadAllText(_xmlFilePath).Trim();
        }
        public XDocument LoadXDocument() {
            return XDocument.Parse(LoadXmlString());
        }
        public override string ToString() {
            return GetXmlFileName();
        }

        public static IEnumerable<object> Collect() {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");
            var dir = Path.Combine(root, "DataPatterns");
            if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);
            foreach (var file in Directory.GetFiles(dir)) {
                yield return new object[] { new DataPattern(file) };
            }
        }
    }


    [System.AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class UseDataPatternsAttribute : TestCaseSourceAttribute {
        public UseDataPatternsAttribute() : base(typeof(DataPattern), nameof(DataPattern.Collect)) {

        }
    }
}
