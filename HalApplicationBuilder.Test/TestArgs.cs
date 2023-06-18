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
        public HalappProject CreateProject() {
            var project = HalappProject.Create(
                applicationName: LoadXDocument().Root!.Name.LocalName,
                verbose: false,
                keepTempIferror: false,
                log: Console.Out);

            var xmlPath = project.GetAggregateSchemaPath();
            File.WriteAllText(xmlPath, LoadXmlString());

            project.EnsureCreateRuntimeSettingFile();

            return project;
        }
    }
}
