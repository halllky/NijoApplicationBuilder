using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;

namespace Nijo.IntegrationTest.Implementors;

public class DoubleImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "011_ダブル.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

public partial class OverridedApplicationService {
    public override Task<CommandModelテストReturnType> Execute(CommandModelテストParameter param, IPresentationContext<CommandModelテストParameterMessages> context) {
        throw new NotImplementedException();
    }

    protected override IQueryable<顧客SearchResult> CreateQuerySource(顧客SearchCondition searchCondition, IPresentationContext<顧客Messages> context) {
        return DbContext.顧客DbSet.Select(e => new 顧客SearchResult {
            顧客ID = e.顧客ID,
            顧客名 = e.顧客名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<売上SearchResult> CreateQuerySource(売上SearchCondition searchCondition, IPresentationContext<売上Messages> context) {
        return DbContext.売上DbSet.Select(e => new 売上SearchResult {
            売上ID = e.売上ID,
            売上金額 = e.売上金額,
            売上日 = e.売上日,
            顧客_顧客ID = e.顧客!.顧客ID,
            顧客_顧客名 = e.顧客!.顧客名,
            Version = (int)e.Version!,
        });
    }
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(117));

        // ルート関連
        AssertExists<InstanceValueProperty>(properties, "ルート.ID1");
        AssertExists<InstanceValueProperty>(properties, "ルート.名前1");

        // 子関連（Child）
        AssertExists<InstanceValueProperty>(properties, "ルート.子_ID2");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_名前2");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_子の子_ID3");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_子の子_名前3");

        // 子の子配列（Children）
        AssertExists<InstanceStructureProperty>(properties, "ルート.子_子の子配列");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_子の子配列.Select(e => e.ID4)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子_子の子配列.Select(e => e.名前4)");

        // 子配列関連（Children）
        AssertExists<InstanceStructureProperty>(properties, "ルート.子配列");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.ID7)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.名前7)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.子配列の子_ID8)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.Select(e => e.子配列の子_名前8)");

        // 子配列の子配列（Children）
        AssertExists<InstanceStructureProperty>(properties, "ルート.子配列.SelectMany(e => e.子配列の子配列)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.SelectMany(e => e.子配列の子配列).Select(e => e.ID9)");
        AssertExists<InstanceValueProperty>(properties, "ルート.子配列.SelectMany(e => e.子配列の子配列).Select(e => e.名前9)");

        // ルートを参照1関連
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_ID1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_名前1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_ID2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_名前2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_子の子_ID3");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_子の子_名前3");
        AssertExists<InstanceStructureProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_子の子配列");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_子の子配列.Select(e => e.ID4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_Parent_子_子の子配列.Select(e => e.名前4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_ID7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_名前7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_子配列の子_ID8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_Parent_子配列の子_名前8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.ルート子配列子配列_名前9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.名前100");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_Parent_ID1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_Parent_名前1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_Parent_子_ID2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_Parent_子_名前2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_Parent_子_子の子_ID3");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_Parent_子_子の子_名前3");
        AssertExists<InstanceStructureProperty>(properties, "ルートを参照1.キーでないルート_Parent_Parent_子_子の子配列");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_Parent_子_子の子配列.Select(e => e.ID4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_Parent_子_子の子配列.Select(e => e.名前4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_ID7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_名前7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_子配列の子_ID8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_Parent_子配列の子_名前8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照1.キーでないルート_名前9");

        // ルートを参照2関連
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_Parent_ID1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_Parent_名前1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_Parent_子_ID2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_Parent_子_名前2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_Parent_子_子の子_ID3");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_Parent_子_子の子_名前3");
        AssertExists<InstanceStructureProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_Parent_子_子の子配列");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_Parent_子_子の子配列.Select(e => e.ID4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_Parent_子_子の子配列.Select(e => e.名前4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_ID7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_名前7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_子配列の子_ID8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_Parent_子配列の子_名前8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_ルート子配列子配列_名前9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_名前100");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_Parent_ID1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_Parent_名前1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_Parent_子_ID2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_Parent_子_名前2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_Parent_子_子の子_ID3");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_Parent_子_子の子_名前3");
        AssertExists<InstanceStructureProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_Parent_子_子の子配列");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_Parent_子_子の子配列.Select(e => e.ID4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_Parent_子_子の子配列.Select(e => e.名前4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_ID7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_名前7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_子配列の子_ID8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_Parent_子配列の子_名前8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.中継_キーでないルート_名前9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.名前201");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_Parent_ID1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_Parent_名前1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_Parent_子_ID2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_Parent_子_名前2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_Parent_子_子の子_ID3");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_Parent_子_子の子_名前3");
        AssertExists<InstanceStructureProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_Parent_子_子の子配列");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_Parent_子_子の子配列.Select(e => e.ID4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_Parent_子_子の子配列.Select(e => e.名前4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_ID7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_名前7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_子配列の子_ID8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_Parent_子配列の子_名前8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_ルート子配列子配列_名前9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_名前100");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_Parent_ID1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_Parent_名前1");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_Parent_子_ID2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_Parent_子_名前2");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_Parent_子_子の子_ID3");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_Parent_子_子の子_名前3");
        AssertExists<InstanceStructureProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_Parent_子_子の子配列");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_Parent_子_子の子配列.Select(e => e.ID4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_Parent_子_子の子配列.Select(e => e.名前4)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_ID7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_名前7");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_子配列の子_ID8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_Parent_子配列の子_名前8");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_ID9");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.キーでない中継_キーでないルート_名前9");
        AssertExists<InstanceStructureProperty>(properties, "ルートを参照2.参照2の子配列");

        // 参照2の子配列関連
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.参照2の子配列.Select(e => e.参照2子配列ID)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.参照2の子配列.Select(e => e.名前202)");
        AssertExists<InstanceStructureProperty>(properties, "ルートを参照2.参照2の子配列.SelectMany(e => e.参照2の子配列の子配列)");

        // 参照2の子配列の子配列関連
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.参照2の子配列.SelectMany(e => e.参照2の子配列の子配列).Select(e => e.参照2子配列子配列ID)");
        AssertExists<InstanceValueProperty>(properties, "ルートを参照2.参照2の子配列.SelectMany(e => e.参照2の子配列の子配列).Select(e => e.名前203)");
    }
}
