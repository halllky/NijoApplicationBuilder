using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;

namespace Nijo.IntegrationTest.Implementors;

public class PrimaryKeyRefImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "013_主キーにref.xml";

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

    protected override IQueryable<主SearchResult> CreateQuerySource(主SearchCondition searchCondition, IPresentationContext<主Messages> context) {
        return DbContext.主DbSet.Select(e => new 主SearchResult {
            主ID = e.主ID,
            主名 = e.主名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<従SearchResult> CreateQuerySource(従SearchCondition searchCondition, IPresentationContext<従Messages> context) {
        return DbContext.従DbSet.Select(e => new 従SearchResult {
            従ID = e.従ID,
            従名 = e.従名,
            主_主ID = e.主!.主ID,
            主_主名 = e.主!.主名,
            Version = (int)e.Version!,
        });
    }
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(54));

        // 受注関連
        AssertExists<InstanceValueProperty>(properties, "受注.ID");
        AssertExists<InstanceValueProperty>(properties, "受注.表示名称");
        AssertExists<InstanceValueProperty>(properties, "受注.受注日");
        AssertExists<InstanceStructureProperty>(properties, "受注.明細");

        // 明細の子要素
        AssertExists<InstanceValueProperty>(properties, "受注.明細.Select(e => e.連番)");
        AssertExists<InstanceValueProperty>(properties, "受注.明細.Select(e => e.商品名)");
        AssertExists<InstanceValueProperty>(properties, "受注.明細.Select(e => e.数量)");

        // 納品関連
        AssertExists<InstanceValueProperty>(properties, "納品.受注明細_連番");
        AssertExists<InstanceValueProperty>(properties, "納品.受注明細_商品名");
        AssertExists<InstanceValueProperty>(properties, "納品.受注明細_数量");
        AssertExists<InstanceValueProperty>(properties, "納品.受注明細_Parent_ID");
        AssertExists<InstanceValueProperty>(properties, "納品.受注明細_Parent_表示名称");
        AssertExists<InstanceValueProperty>(properties, "納品.受注明細_Parent_受注日");
        AssertExists<InstanceValueProperty>(properties, "納品.表示名称");
        AssertExists<InstanceValueProperty>(properties, "納品.納品日");
        AssertExists<InstanceValueProperty>(properties, "納品.納品数量");
        AssertExists<InstanceStructureProperty>(properties, "納品.備考");

        // 備考の子要素
        AssertExists<InstanceValueProperty>(properties, "納品.備考.Select(e => e.備考連番)");
        AssertExists<InstanceValueProperty>(properties, "納品.備考.Select(e => e.本文)");

        // 請求関連
        AssertExists<InstanceValueProperty>(properties, "請求.納品_受注明細_連番");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_受注明細_商品名");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_受注明細_数量");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_受注明細_Parent_ID");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_受注明細_Parent_表示名称");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_受注明細_Parent_受注日");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_表示名称");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_納品日");
        AssertExists<InstanceValueProperty>(properties, "請求.納品_納品数量");
        AssertExists<InstanceStructureProperty>(properties, "請求.納品_備考");
        AssertExists<InstanceValueProperty>(properties, "請求.表示名称");
        AssertExists<InstanceValueProperty>(properties, "請求.金額");

        // 入金関連
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_受注明細_連番");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_受注明細_商品名");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_受注明細_数量");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_受注明細_Parent_ID");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_受注明細_Parent_表示名称");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_受注明細_Parent_受注日");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_表示名称");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_納品日");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_納品_納品数量");
        AssertExists<InstanceStructureProperty>(properties, "入金.請求_納品_備考");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_表示名称");
        AssertExists<InstanceValueProperty>(properties, "入金.請求_金額");
        AssertExists<InstanceValueProperty>(properties, "入金.表示名称");
        AssertExists<InstanceValueProperty>(properties, "入金.金額");

        // 状況2関連
        AssertExists<InstanceValueProperty>(properties, "状況2.受注_ID");
        AssertExists<InstanceValueProperty>(properties, "状況2.受注_表示名称");
        AssertExists<InstanceValueProperty>(properties, "状況2.受注_受注日");
        AssertExists<InstanceStructureProperty>(properties, "状況2.受注_明細");
        AssertExists<InstanceValueProperty>(properties, "状況2.受注数");
        AssertExists<InstanceValueProperty>(properties, "状況2.納品数");
        AssertExists<InstanceValueProperty>(properties, "状況2.請求額合計");
        AssertExists<InstanceValueProperty>(properties, "状況2.入金済額合計");
        AssertExists<InstanceValueProperty>(properties, "状況2.ステータス");
    }
}
