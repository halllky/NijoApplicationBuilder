using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;

namespace Nijo.IntegrationTest.Implementors;

public class MultiplePathRefImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "014_複数経路の参照.xml";

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

    protected override IQueryable<親SearchResult> CreateQuerySource(親SearchCondition searchCondition, IPresentationContext<親Messages> context) {
        return DbContext.親DbSet.Select(e => new 親SearchResult {
            親ID = e.親ID,
            親名 = e.親名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<子SearchResult> CreateQuerySource(子SearchCondition searchCondition, IPresentationContext<子Messages> context) {
        return DbContext.子DbSet.Select(e => new 子SearchResult {
            子ID = e.子ID,
            子名 = e.子名,
            親_親ID = e.親!.親ID,
            親_親名 = e.親!.親名,
            Version = (int)e.Version!,
        });
    }
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(64));

        // 営業区関連
        AssertExists<InstanceValueProperty>(properties, "営業区.営業区ID");
        AssertExists<InstanceValueProperty>(properties, "営業区.営業区名");
        AssertExists<InstanceStructureProperty>(properties, "営業区.部署");

        // 部署関連
        AssertExists<InstanceValueProperty>(properties, "営業区.部署.Select(e => e.部署ID)");
        AssertExists<InstanceValueProperty>(properties, "営業区.部署.Select(e => e.部署名)");

        // 依頼関連
        AssertExists<InstanceValueProperty>(properties, "依頼.依頼番号");
        AssertExists<InstanceValueProperty>(properties, "依頼.発注部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "依頼.発注部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "依頼.発注部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "依頼.発注部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "依頼.最終承認部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "依頼.最終承認部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "依頼.最終承認部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "依頼.最終承認部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "依頼.監督部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "依頼.監督部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "依頼.監督部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "依頼.監督部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "依頼.依頼内容");

        // 従業員ステータス
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.従業員ID");

        // 従業員ステータス.担当中の作業（ref-to:依頼）
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_依頼番号");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_発注部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_発注部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_発注部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_発注部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_最終承認部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_最終承認部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_最終承認部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_最終承認部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_監督部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_監督部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_監督部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_監督部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.担当中の作業_依頼内容");

        // 従業員ステータス.来月の作業（ref-to:依頼）
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_依頼番号");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_発注部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_発注部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_発注部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_発注部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_最終承認部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_最終承認部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_最終承認部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_最終承認部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_監督部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_監督部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_監督部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_監督部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.来月の作業_依頼内容");

        // 従業員ステータス.再来月の作業（ref-to:依頼）
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_依頼番号");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_発注部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_発注部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_発注部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_発注部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_最終承認部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_最終承認部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_最終承認部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_最終承認部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_監督部署_部署ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_監督部署_部署名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_監督部署_Parent_営業区ID");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_監督部署_Parent_営業区名");
        AssertExists<InstanceValueProperty>(properties, "従業員ステータス.再来月の作業_依頼内容");
    }
}
