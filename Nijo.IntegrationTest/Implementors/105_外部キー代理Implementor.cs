using Nijo.CodeGenerating;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class ForeignKeyProxyImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "105_外部キー代理.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
// このテストではオブジェクトパスの検証のみ行うため、実装は空で良い
namespace MyApp.Core;
partial class OverridedApplicationService {
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        // 会社モデル
        AssertExists<InstanceValueProperty>(properties, "会社.会社ID");
        AssertExists<InstanceValueProperty>(properties, "会社.会社名");

        // 部署モデル
        AssertExists<InstanceValueProperty>(properties, "部署.会社_会社ID");
        AssertExists<InstanceValueProperty>(properties, "部署.会社_会社名");
        AssertExists<InstanceValueProperty>(properties, "部署.営業区ID");
        AssertExists<InstanceValueProperty>(properties, "部署.部署ID");
        AssertExists<InstanceValueProperty>(properties, "部署.部署名");

        // 従業員モデル
        AssertExists<InstanceValueProperty>(properties, "従業員.会社_会社ID");
        AssertExists<InstanceValueProperty>(properties, "従業員.会社_会社名");
        AssertExists<InstanceValueProperty>(properties, "従業員.従業員ID");
        AssertExists<InstanceValueProperty>(properties, "従業員.従業員名");

        AssertExists<InstanceStructureProperty>(properties, "従業員.所属部署情報");
        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.年度)");

        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.本務部署_会社_会社ID)");
        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.本務部署_会社_会社名)");
        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.本務部署_営業区ID)");
        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.本務部署_部署ID)");
        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.本務部署_部署名)");

        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.兼務部署_会社_会社ID)");
        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.兼務部署_会社_会社名)");
        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.兼務部署_営業区ID)");
        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.兼務部署_部署ID)");
        AssertExists<InstanceValueProperty>(properties, "従業員.所属部署情報.Select(e => e.兼務部署_部署名)");

        // 集約A
        AssertExists<InstanceValueProperty>(properties, "集約A.会社ID");
        AssertExists<InstanceValueProperty>(properties, "集約A.営業区ID");
        AssertExists<InstanceValueProperty>(properties, "集約A.部署ID");
        AssertExists<InstanceValueProperty>(properties, "集約A.部署名");

        // 集約B
        AssertExists<InstanceValueProperty>(properties, "集約B.会社ID");
        AssertExists<InstanceValueProperty>(properties, "集約B.従業員ID");
        AssertExists<InstanceValueProperty>(properties, "集約B.従業員名");

        // 集約C
        AssertExists<InstanceValueProperty>(properties, "集約C.会社ID");
        AssertExists<InstanceValueProperty>(properties, "集約C.集約A_会社ID");
        AssertExists<InstanceValueProperty>(properties, "集約C.集約A_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "集約C.集約A_部署ID");
        AssertExists<InstanceValueProperty>(properties, "集約C.集約A_部署名");

        AssertExists<InstanceValueProperty>(properties, "集約C.集約B_会社ID");
        AssertExists<InstanceValueProperty>(properties, "集約C.集約B_従業員ID");
        AssertExists<InstanceValueProperty>(properties, "集約C.集約B_従業員名");

        AssertExists<InstanceValueProperty>(properties, "集約C.非キー項目");

        // 顧客
        AssertExists<InstanceValueProperty>(properties, "顧客.リージョン");
        AssertExists<InstanceValueProperty>(properties, "顧客.顧客ID");
        AssertExists<InstanceValueProperty>(properties, "顧客.顧客名");

        // パッケージ
        AssertExists<InstanceValueProperty>(properties, "パッケージ.リージョン");
        AssertExists<InstanceValueProperty>(properties, "パッケージ.パッケージID");
        AssertExists<InstanceValueProperty>(properties, "パッケージ.パッケージ名");

        // エディション
        AssertExists<InstanceValueProperty>(properties, "エディション.リージョン");
        AssertExists<InstanceValueProperty>(properties, "エディション.パッケージ_リージョン");
        AssertExists<InstanceValueProperty>(properties, "エディション.パッケージ_パッケージID");
        AssertExists<InstanceValueProperty>(properties, "エディション.パッケージ_パッケージ名");
        AssertExists<InstanceValueProperty>(properties, "エディション.名前");

        // ライセンス
        AssertExists<InstanceValueProperty>(properties, "ライセンス.リージョン");

        AssertExists<InstanceValueProperty>(properties, "ライセンス.顧客_リージョン");
        AssertExists<InstanceValueProperty>(properties, "ライセンス.顧客_顧客ID");
        AssertExists<InstanceValueProperty>(properties, "ライセンス.顧客_顧客名");

        AssertExists<InstanceValueProperty>(properties, "ライセンス.エディション_リージョン");
        AssertExists<InstanceValueProperty>(properties, "ライセンス.エディション_パッケージ_リージョン");
        AssertExists<InstanceValueProperty>(properties, "ライセンス.エディション_パッケージ_パッケージID");
        AssertExists<InstanceValueProperty>(properties, "ライセンス.エディション_パッケージ_パッケージ名");
        AssertExists<InstanceValueProperty>(properties, "ライセンス.エディション_名前");

        AssertExists<InstanceValueProperty>(properties, "ライセンス.契約開始日");
    }
}
