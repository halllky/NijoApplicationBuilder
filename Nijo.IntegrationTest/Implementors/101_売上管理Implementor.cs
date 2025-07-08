using Nijo.CodeGenerating;
using NUnit.Framework;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class SalesManagementImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "101_売上管理.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
// このテストではオブジェクトパスの検証のみ行うため、実装は空で良い
namespace MyApp.Core;
partial class OverridedApplicationService {
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        // 商品モデル
        AssertExists<InstanceValueProperty>(properties, "商品.商品ID");
        AssertExists<InstanceValueProperty>(properties, "商品.商品名");

        // 受注モデル
        AssertExists<InstanceValueProperty>(properties, "受注.受注ID");
        AssertExists<InstanceValueProperty>(properties, "受注.受注日");
        AssertExists<InstanceStructureProperty>(properties, "受注.受注明細");
        AssertExists<InstanceValueProperty>(properties, "受注.受注明細.Select(e => e.商品_商品ID)");
        AssertExists<InstanceValueProperty>(properties, "受注.受注明細.Select(e => e.商品_商品名)");
        AssertExists<InstanceValueProperty>(properties, "受注.受注明細.Select(e => e.数量)");
        AssertExists<InstanceValueProperty>(properties, "受注.受注明細.Select(e => e.単価)");

        // 納品モデル
        AssertExists<InstanceValueProperty>(properties, "納品.商品ID");
        AssertExists<InstanceValueProperty>(properties, "納品.納品日");
        AssertExists<InstanceStructureProperty>(properties, "納品.納品明細");
        AssertExists<InstanceValueProperty>(properties, "納品.納品明細.Select(e => e.ID)");
        AssertExists<InstanceValueProperty>(properties, "納品.納品明細.Select(e => e.商品_商品ID)");
        AssertExists<InstanceValueProperty>(properties, "納品.納品明細.Select(e => e.商品_商品名)");
        AssertExists<InstanceValueProperty>(properties, "納品.納品明細.Select(e => e.数量)");
        AssertExists<InstanceValueProperty>(properties, "納品.納品明細.Select(e => e.受注_受注ID)");
        AssertExists<InstanceValueProperty>(properties, "納品.納品明細.Select(e => e.受注_受注日)");

        // 請求モデル
        AssertExists<InstanceValueProperty>(properties, "請求.請求ID");
        AssertExists<InstanceValueProperty>(properties, "請求.請求日");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_商品ID");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_納品日");
        AssertExists<InstanceStructureProperty>(properties, "請求.請求明細");
        AssertExists<InstanceValueProperty>(properties, "請求.請求明細.Select(e => e.ID)");
        AssertExists<InstanceValueProperty>(properties, "請求.請求明細.Select(e => e.商品_商品ID)");
        AssertExists<InstanceValueProperty>(properties, "請求.請求明細.Select(e => e.商品_商品名)");
        AssertExists<InstanceValueProperty>(properties, "請求.請求明細.Select(e => e.数量)");
        AssertExists<InstanceValueProperty>(properties, "請求.請求明細.Select(e => e.単価)");

        // 入金モデル
        AssertExists<InstanceValueProperty>(properties, "入金.入金ID");
        AssertExists<InstanceValueProperty>(properties, "入金.入金日");
        AssertExists<InstanceValueProperty>(properties, "入金.金額");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_請求ID");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_請求日");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_商品ID");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_納品日");

        // 受注から入金までモデル
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注ID");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注日");
        AssertExists<InstanceStructureProperty>(properties, "受注から入金まで.受注明細2");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.商品_商品ID)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.商品_商品名)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.数量)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.単価)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.納品2_商品ID)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.納品2_納品日)");
        AssertExists<InstanceStructureProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.納品2_納品明細2)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.納品2_納品明細2).Select(e => e.ID)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.納品2_納品明細2).Select(e => e.商品_商品ID)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.納品2_納品明細2).Select(e => e.商品_商品名)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.納品2_納品明細2).Select(e => e.数量)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.請求2_請求ID)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.請求2_請求日)");
        AssertExists<InstanceStructureProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.請求2_請求明細2)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.請求2_請求明細2).Select(e => e.ID)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.請求2_請求明細2).Select(e => e.商品_商品ID)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.請求2_請求明細2).Select(e => e.商品_商品名)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.請求2_請求明細2).Select(e => e.数量)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.SelectMany(e => e.請求2_請求明細2).Select(e => e.単価)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.入金2_入金ID)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.入金2_入金日)");
        AssertExists<InstanceValueProperty>(properties, "受注から入金まで.受注明細2.Select(e => e.入金2_金額)");
    }
}
