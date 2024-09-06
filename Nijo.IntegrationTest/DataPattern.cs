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

namespace Nijo.IntegrationTest {
    /// <summary>
    /// テストデータパターンXML
    /// </summary>
    public abstract class DataPattern {
        protected DataPattern(string xmlFileName) {
            _xmlFileName = xmlFileName;
        }

        private readonly string _xmlFileName;

        public string LoadXmlString() {
            var testProjectRoot = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..");
            var xmlFullPath = Path.Combine(testProjectRoot, "DataPatterns", _xmlFileName);
            return File.ReadAllText(xmlFullPath).Trim();
        }
        public override string ToString() {
            return Path.GetFileName(_xmlFileName);
        }

        /// <summary>
        /// NUnitの <see cref="TestCaseSourceAttribute"/> がこのプロジェクト中にあるテストパターンの一覧を収集するのに必要なメソッド
        /// </summary>
        public static IEnumerable<object> Collect() {
            // テストプロジェクト中にある、このクラスを継承している型のインスタンスを集める
            return Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => typeof(DataPattern).IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => Activator.CreateInstance(type)!);
        }
    }


    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class UseDataPatternsAttribute : TestCaseSourceAttribute {
        public UseDataPatternsAttribute() : base(typeof(DataPattern), nameof(DataPattern.Collect)) {

        }
    }
}
