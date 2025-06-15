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
        // 従業員モデル
        AssertExists<InstanceValueProperty>(properties, "従業員.従業員コード");
        AssertExists<InstanceValueProperty>(properties, "従業員.名前");

        // 資産取得モデル
        AssertExists<InstanceValueProperty>(properties, "資産取得.資産ID");
        AssertExists<InstanceValueProperty>(properties, "資産取得.名前");
        AssertExists<InstanceValueProperty>(properties, "資産取得.種別");
        AssertExists<InstanceValueProperty>(properties, "資産取得.購入日");
        AssertExists<InstanceValueProperty>(properties, "資産取得.取得価額");
        AssertExists<InstanceValueProperty>(properties, "資産取得.管理者_従業員コード");
        AssertExists<InstanceValueProperty>(properties, "資産取得.管理者_名前");

        // 資産移動モデル
        AssertExists<InstanceValueProperty>(properties, "資産移動.ID");
        AssertExists<InstanceValueProperty>(properties, "資産移動.資産_資産ID");
        AssertExists<InstanceValueProperty>(properties, "資産移動.資産_名前");
        AssertExists<InstanceValueProperty>(properties, "資産移動.資産_種別");
        AssertExists<InstanceValueProperty>(properties, "資産移動.資産_購入日");
        AssertExists<InstanceValueProperty>(properties, "資産移動.資産_取得価額");
        AssertExists<InstanceValueProperty>(properties, "資産移動.資産_管理者_従業員コード");
        AssertExists<InstanceValueProperty>(properties, "資産移動.資産_管理者_名前");
        AssertExists<InstanceValueProperty>(properties, "資産移動.移動日");
        AssertExists<InstanceValueProperty>(properties, "資産移動.移動後管理者_従業員コード");
        AssertExists<InstanceValueProperty>(properties, "資産移動.移動後管理者_名前");

        // 資産(クエリーモデル)
        AssertExists<InstanceValueProperty>(properties, "資産.資産ID");
        AssertExists<InstanceValueProperty>(properties, "資産.名前");
        AssertExists<InstanceValueProperty>(properties, "資産.種別");
        AssertExists<InstanceStructureProperty>(properties, "資産.履歴");
        AssertExists<InstanceValueProperty>(properties, "資産.履歴.Select(e => e.連番)");
        AssertExists<InstanceValueProperty>(properties, "資産.履歴.Select(e => e.期間FROM)");
        AssertExists<InstanceValueProperty>(properties, "資産.履歴.Select(e => e.期間TO)");
        AssertExists<InstanceValueProperty>(properties, "資産.履歴.Select(e => e.管理者_従業員コード)");
        AssertExists<InstanceValueProperty>(properties, "資産.履歴.Select(e => e.管理者_名前)");
    }
}
