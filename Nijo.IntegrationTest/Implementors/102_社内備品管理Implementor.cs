using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class EquipmentManagementImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "102_社内備品管理.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
// このテストではオブジェクトパスの検証のみ行うため、実装は空で良い
namespace MyApp.Core;
partial class OverridedApplicationService {
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        // TODO: 102_社内備品管理.xmlのスキーマ定義に基づいてアサーションを記述する
        Assert.Fail("102_社内備品管理 の AssertSearchResultMemberPath が実装されていません。");
    }
}
