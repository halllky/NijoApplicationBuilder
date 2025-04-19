using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class RequiredAttributeTestImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "104_required属性のテスト.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
// このテストではオブジェクトパスの検証のみ行うため、実装は空で良い
namespace MyApp.Core;
partial class OverridedApplicationService {
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        // TODO: 104_required属性のテスト.xmlのスキーマ定義に基づいてアサーションを記述する
        Assert.Fail("104_required属性のテスト の AssertSearchResultMemberPath が実装されていません。");
    }
}
