using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        public E_DataPattern AsEnum() {
            var basename = Path.GetFileName(_xmlFilePath);
            foreach (var member in typeof(E_DataPattern).GetMembers()) {
                if (member.GetCustomAttribute<FileNameAttribute>()?.Value == basename) {
                    return Enum.Parse<E_DataPattern>(member.Name);
                }
            }
            return (E_DataPattern)(-1);
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
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class FileNameAttribute : TestCaseSourceAttribute {
        public FileNameAttribute(string value) : base(typeof(DataPattern), nameof(DataPattern.Collect)) {
            Value = value;
        }
        public string Value { get; }
    }
}
