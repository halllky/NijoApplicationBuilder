using Nijo.CodeGenerating;
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
        // 課題モデル
        AssertExists<InstanceValueProperty>(properties, "課題.課題番号");
        AssertExists<InstanceValueProperty>(properties, "課題.概要");
        AssertExists<InstanceValueProperty>(properties, "課題.見積工数D");
        AssertExists<InstanceValueProperty>(properties, "課題.プロジェクト全体課題_結論");
        AssertExists<InstanceValueProperty>(properties, "課題.プロジェクト全体課題_この課題について話し合われたセッションの日付");

        AssertExists<InstanceValueProperty>(properties, "課題.個別チーム課題_担当者_アカウントID");
        AssertExists<InstanceValueProperty>(properties, "課題.個別チーム課題_担当者_アカウント種別");
        AssertExists<InstanceValueProperty>(properties, "課題.個別チーム課題_担当者_アカウント名");

        AssertExists<InstanceStructureProperty>(properties, "課題.個別チーム課題_チェックポイント");
        AssertExists<InstanceValueProperty>(properties, "課題.個別チーム課題_チェックポイント.Select(e => e.ID)");
        AssertExists<InstanceValueProperty>(properties, "課題.個別チーム課題_チェックポイント.Select(e => e.完了)");
        AssertExists<InstanceValueProperty>(properties, "課題.個別チーム課題_チェックポイント.Select(e => e.観点)");

        // アカウントモデル
        AssertExists<InstanceValueProperty>(properties, "アカウント.アカウントID");
        AssertExists<InstanceValueProperty>(properties, "アカウント.アカウント種別");
        AssertExists<InstanceValueProperty>(properties, "アカウント.アカウント名");
    }
}
