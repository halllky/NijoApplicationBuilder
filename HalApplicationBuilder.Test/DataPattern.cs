using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit.Abstractions;

namespace HalApplicationBuilder.Test {
    public class DataPattern : IXunitSerializable {

        public string XmlFilePath { get; set; } = string.Empty;

        public string GetXmlFileName() {
            return string.IsNullOrWhiteSpace(XmlFilePath)
                ? string.Empty
                : Path.GetFileName(XmlFilePath);
        }
        public string LoadXmlString() {
            return string.IsNullOrWhiteSpace(XmlFilePath)
                ? string.Empty
                : File.ReadAllText(XmlFilePath).Trim();
        }
        public XDocument LoadXDocument() {
            return XDocument.Parse(LoadXmlString());
        }

        public void Deserialize(IXunitSerializationInfo info) {
            XmlFilePath = info.GetValue<string>(nameof(XmlFilePath));
        }

        public void Serialize(IXunitSerializationInfo info) {
            info.AddValue(nameof(XmlFilePath), XmlFilePath);
        }

        public override string ToString() {
            return GetXmlFileName();
        }
    }
}
