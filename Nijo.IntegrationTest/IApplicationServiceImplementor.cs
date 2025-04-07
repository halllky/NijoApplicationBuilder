using System.Xml.Linq;

namespace Nijo.IntegrationTest;

public interface IApplicationServiceImplementor {
    /// <summary>
    /// この実装が対応するXMLファイルの名前を返します。
    /// </summary>
    string TargetXmlFileName { get; }

    /// <summary>
    /// OverridedApplicationServiceの実装を返します。
    /// </summary>
    string GetImplementation(XDocument schemaXml);
}
