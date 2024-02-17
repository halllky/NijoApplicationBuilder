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
    public class DataPattern {

        #region XMLファイル名
        public const string FILENAME_000 = "000_単純な集約.xml";
        public const string FILENAME_001 = "001_Refのみ.xml";
        public const string FILENAME_002 = "002_Childrenのみ.xml";
        public const string FILENAME_003 = "003_Childのみ.xml";
        public const string FILENAME_004 = "004_Variationのみ.xml";
        public const string FILENAME_010 = "010_ChildrenからChildrenへの参照.xml";
        public const string FILENAME_011 = "011_ダブル.xml";
        public const string FILENAME_012 = "012_スカラメンバー網羅.xml";
        public const string FILENAME_013 = "013_主キーにRef.xml";
        public const string FILENAME_100 = "100_RDRA.xml";
        public const string FILENAME_101 = "101_売上管理.xml";
        #endregion XMLファイル名


        private DataPattern(string xmlFilePath) {
            _xmlFilePath = xmlFilePath;
        }
        public static DataPattern FromFileName(string pattern) {
            var xmlFilePath = Path.Combine(DataPatternsDir(), pattern);
            return new DataPattern(xmlFilePath);
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

        private static string DataPatternsDir() {
            var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..");
            return Path.Combine(root, "DataPatterns");
        }
        public static IEnumerable<object> Collect() {
            foreach (var file in Directory.GetFiles(DataPatternsDir())) {
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
