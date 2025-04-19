using Nijo.CodeGenerating.Helpers;
using System.Xml.Linq;

namespace Nijo.IntegrationTest;

public abstract class ApplicationServiceImplementorBase {
    /// <summary>
    /// この実装が対応するXMLファイルの名前を返します。
    /// </summary>
    public abstract string TargetXmlFileName { get; }

    /// <summary>
    /// OverridedApplicationServiceの実装を返します。
    /// </summary>
    public abstract string GetImplementation(XDocument schemaXml);
}
