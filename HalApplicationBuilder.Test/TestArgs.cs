using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Test {
    public class TestArgs {
        public TestArgs(string xmlFilePath) {
            _xmlFilePath = xmlFilePath;
        }
        private readonly string _xmlFilePath;

        public string LoadXmlString() {
            return File.ReadAllText(_xmlFilePath).Trim();
        }
        public XDocument LoadXDocument() {
            return XDocument.Parse(LoadXmlString());
        }
        public HalappProject OpenProject() {
            // 依存先パッケージのインストールにかかる時間とデータ量を削減するために全テストで1つのディレクトリを共有する
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "DIST_PROJECT");
            var project = Directory.Exists(dir)
                ? HalappProject.Open(dir, log: Console.Out)
                : HalappProject.Create(dir, LoadXDocument().Root!.Name.LocalName, false, log: Console.Out);

            var xmlPath = project.GetAggregateSchemaPath();
            File.WriteAllText(xmlPath, LoadXmlString());

            project.EnsureCreateRuntimeSettingFile();

            return project;
        }
    }
}
