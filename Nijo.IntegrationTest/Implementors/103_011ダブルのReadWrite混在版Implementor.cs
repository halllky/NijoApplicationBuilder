using Nijo.CodeGenerating;
using NUnit.Framework;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class DoubleReadWriteImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "103_011ダブルのReadWrite混在版.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
// このテストではオブジェクトパスの検証のみ行うため、実装は空で良い
namespace MyApp.Core;
partial class OverridedApplicationService {
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        // ルートモデル
        AssertExists<InstanceValueProperty>(properties, "ルート.ID1");
        AssertExists<InstanceValueProperty>(properties, "ルート.名前1");

        // 子関連
        AssertExists<InstanceValueProperty>(properties, "ルート.子_名前2");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_子の子_名前3");
        AssertExists<InstanceStructureProperty>(properties, "ルート.子_子の子配列");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_子の子配列.Select(e => e.ID4)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_子の子配列.Select(e => e.名前4)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_子の子バリエーションA_名前5");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_子の子バリエーションB_名前6");

        // 子配列関連
        AssertExists<InstanceStructureProperty>(properties, "ルート.子配列");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.ID7)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.名前7)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.子配列の子_名前8)");

        AssertExists<InstanceStructureProperty>(properties, "ルート.子配列.Select(e => e.子配列の子配列)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.子配列の子配列.Select(e2 => e2.ID9))");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.子配列の子配列.Select(e2 => e2.名前9))");

        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.子配列の子バリエーションA_名前10)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.子配列の子バリエーションB_名前11)");

        // 子バリエーション種別A関連
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別A_名前12");
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別A_子バリエーション種別Aの子_名前13");

        AssertExists<InstanceStructureProperty>(properties, "ルート.子バリエーション種別A_子バリエーション種別Aの子配列");
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別A_子バリエーション種別Aの子配列.Select(e => e.ID14)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別A_子バリエーション種別Aの子配列.Select(e => e.名前14)");

        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別A_子バリエーション種別Aの子バリエーションA_名前15");
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別A_子バリエーション種別Aの子バリエーションB_名前16");

        // 子バリエーション種別B関連
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別B_名前17");
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別B_子バリエーション種別Bの子_名前18");

        AssertExists<InstanceStructureProperty>(properties, "ルート.子バリエーション種別B_子バリエーション種別Bの子配列");
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別B_子バリエーション種別Bの子配列.Select(e => e.ID19)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別B_子バリエーション種別Bの子配列.Select(e => e.名前19)");

        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別B_子バリエーション種別Bの子バリエーションA_名前20");
        AssertExists<InstanceValueProperty>(properties, "ルート.子バリエーション種別B_子バリエーション種別Bの子バリエーションB_名前21");

        // ルートを参照1モデル
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_名前9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_ID7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_名前7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_子配列の子_名前8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_子配列の子バリエーションA_名前10");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_子配列の子バリエーションB_名前11");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_ID1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_名前1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_名前2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_子の子_名前3");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_子の子バリエーションA_名前5");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_子の子バリエーションB_名前6");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子バリエーション種別A_名前12");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子バリエーション種別A_子バリエーション種別Aの子_名前13");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子バリエーション種別A_子バリエーション種別Aの子バリエーションA_名前15");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子バリエーション種別A_子バリエーション種別Aの子バリエーションB_名前16");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子バリエーション種別B_名前17");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子バリエーション種別B_子バリエーション種別Bの子_名前18");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子バリエーション種別B_子バリエーション種別Bの子バリエーションA_名前20");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子バリエーション種別B_子バリエーション種別Bの子バリエーションB_名前21");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.名前100");

        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_名前9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_ID7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_名前7");

        // ルートを参照2モデル
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_名前9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_名前100");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.名前201");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_名前9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_名前100");

        AssertExists<InstanceStructureProperty>(properties, "ルートを参照2.参照2の子配列");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.参照2の子配列.Select(e => e.参照2子配列ID)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.参照2の子配列.Select(e => e.名前202)");

        AssertExists<InstanceStructureProperty>(properties, "ルートを参照2.参照2の子配列.Select(e => e.参照2の子配列の子配列)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.参照2の子配列.Select(e => e.参照2の子配列の子配列.Select(e2 => e2.参照2子配列子配列ID))");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.参照2の子配列.Select(e => e.参照2の子配列の子配列.Select(e2 => e2.名前203))");
    }
}
