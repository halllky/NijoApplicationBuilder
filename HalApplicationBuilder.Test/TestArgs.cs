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
            using var tokenSource = new CancellationTokenSource();
            var project = HalappProject.Create(
                applicationName: LoadXDocument().Root!.Name.LocalName,
                verbose: false,
                keepTempIferror: false,
                cancellationToken: tokenSource.Token,
                log: Console.Out);

            var xmlPath = project.GetAggregateSchemaPath();
            File.WriteAllText(xmlPath, LoadXmlString());

            project.StartSetup(false, tokenSource.Token)
                   .EnsureCreateRuntimeSettingFile();

            return project;
        }
    }
}