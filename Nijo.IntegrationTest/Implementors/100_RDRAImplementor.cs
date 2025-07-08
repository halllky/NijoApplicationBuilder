using Nijo.CodeGenerating;
// using Nijo.Core.Definition; // 不要なため削除
using NUnit.Framework;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class RDRAImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "100_RDRA.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
// このテストではオブジェクトパスの検証のみ行うため、実装は空で良い
namespace MyApp.Core;
partial class OverridedApplicationService {
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties.Length, Is.EqualTo(20));

        AssertExists<InstanceValueProperty>(properties, "アクター.アクターID");
        AssertExists<InstanceValueProperty>(properties, "アクター.アクター名");
        AssertExists<InstanceValueProperty>(properties, "アクター.人_自社か社外か");

        AssertExists<InstanceValueProperty>(properties, "ユースケース.ユースケースID");
        AssertExists<InstanceValueProperty>(properties, "ユースケース.概要");
        AssertExists<InstanceValueProperty>(properties, "ユースケース.主体_アクターID");
        AssertExists<InstanceValueProperty>(properties, "ユースケース.主体_アクター名");
        AssertExists<InstanceValueProperty>(properties, "ユースケース.主体_人_自社か社外か");

        // ユースケース.関連機能 (Children)
        AssertExists<InstanceStructureProperty>(properties, "ユースケース.関連機能");
        // 子要素のパスを確認
        AssertExists<InstanceValueProperty>(properties, "ユースケース.関連機能.Select(e => e.機能_機能ID)");
        AssertExists<InstanceValueProperty>(properties, "ユースケース.関連機能.Select(e => e.機能_機能名)");
        AssertExists<InstanceValueProperty>(properties, "ユースケース.関連機能.Select(e => e.機能_参照更新の別)");
        AssertExists<InstanceValueProperty>(properties, "ユースケース.関連機能.Select(e => e.機能_非機能要件_レスポンス)");
        AssertExists<InstanceValueProperty>(properties, "ユースケース.関連機能.Select(e => e.機能_非機能要件_セキュリティ)");
        AssertExists<InstanceValueProperty>(properties, "ユースケース.関連機能.Select(e => e.メモ)");

        AssertExists<InstanceValueProperty>(properties, "機能.機能ID");
        AssertExists<InstanceValueProperty>(properties, "機能.機能名");
        AssertExists<InstanceValueProperty>(properties, "機能.参照更新の別");
        AssertExists<InstanceValueProperty>(properties, "機能.非機能要件_レスポンス");
        AssertExists<InstanceValueProperty>(properties, "機能.非機能要件_セキュリティ");
    }
}
