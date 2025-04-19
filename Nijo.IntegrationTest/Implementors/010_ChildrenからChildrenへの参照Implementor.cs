using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;

namespace Nijo.IntegrationTest.Implementors;

public class ChildrenToChildrenRefImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "010_ChildrenからChildrenへの参照.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

public partial class OverridedApplicationService {
    protected override IQueryable<見積書テンプレートSearchResult> CreateQuerySource(見積書テンプレートSearchCondition searchCondition, IPresentationContext<見積書テンプレートMessages> context) {
        return DbContext.見積書テンプレートDbSet.Select(e => new 見積書テンプレートSearchResult {
            テンプレートID = e.テンプレートID,
            テンプレート名 = e.テンプレート名,
            セクション = e.セクション.Select(c => new セクションSearchResult {
                セクションID = c.セクションID,
                セクション名 = c.セクション名,
            }).ToList(),
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<見積書SearchResult> CreateQuerySource(見積書SearchCondition searchCondition, IPresentationContext<見積書Messages> context) {
        return DbContext.見積書DbSet.Select(e => new 見積書SearchResult {
            見積書ID = e.見積書ID,
            タイトル = e.タイトル,
            発行日時 = e.発行日時,
            定型欄 = e.定型欄.Select(c => new 定型欄SearchResult {
                欄ID = c.欄ID,
                セクションID = c.セクションテンプレート_セクションID,
                セクション名 = c.セクションテンプレート!.セクション名,
                文 = c.文,
            }).ToList(),
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<見積回答SearchResult> CreateQuerySource(見積回答SearchCondition searchCondition, IPresentationContext<見積回答Messages> context) {
        return DbContext.見積回答DbSet.Select(e => new 見積回答SearchResult {
            見積書_見積書_見積書ID = e.見積書_見積書ID,
            見積書_見積書_タイトル = e.見積書!.タイトル,
            見積書_見積書_発行日時 = e.見積書!.発行日時,
            返答日 = e.返答日,
            定型欄 = e.見積書.定型欄.Select(c => new 定型欄SearchResult {
                欄ID = c.欄ID,
                セクションID = c.セクションテンプレート_セクションID,
                セクション名 = c.セクションテンプレート!.セクション名,
                文 = c.文,
            }).ToList(),
            コメント = e.コメント.Select(c => new コメントSearchResult {
                欄ID = c.対象!.欄ID,
                セクションID = c.対象!.セクションテンプレート!.セクションID,
                セクション名 = c.対象!.セクションテンプレート!.セクション名,
                文 = c.対象!.文,
                コメント文章 = c.コメント文章,
            }).ToList(),
            Version = (int)e.Version!,
        });
    }
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(31));

        // 見積書テンプレート関連
        AssertExists<InstanceValueProperty>(properties, "見積書テンプレート.テンプレートID");
        AssertExists<InstanceValueProperty>(properties, "見積書テンプレート.テンプレート名");
        AssertExists<InstanceStructureProperty>(properties, "見積書テンプレート.セクション");

        AssertExists<InstanceValueProperty>(properties, "見積書テンプレート.セクション.Select(e => e.セクションID)");
        AssertExists<InstanceValueProperty>(properties, "見積書テンプレート.セクション.Select(e => e.セクション名)");

        // 見積書関連
        AssertExists<InstanceValueProperty>(properties, "見積書.見積書ID");
        AssertExists<InstanceValueProperty>(properties, "見積書.タイトル");
        AssertExists<InstanceValueProperty>(properties, "見積書.発行日時");
        AssertExists<InstanceStructureProperty>(properties, "見積書.定型欄");

        AssertExists<InstanceValueProperty>(properties, "見積書.定型欄.Select(e => e.欄ID)");
        AssertExists<InstanceValueProperty>(properties, "見積書.定型欄.Select(e => e.セクションテンプレート_Parent_テンプレートID)");
        AssertExists<InstanceValueProperty>(properties, "見積書.定型欄.Select(e => e.セクションテンプレート_Parent_テンプレート名)");
        AssertExists<InstanceValueProperty>(properties, "見積書.定型欄.Select(e => e.セクションテンプレート_セクションID)");
        AssertExists<InstanceValueProperty>(properties, "見積書.定型欄.Select(e => e.セクションテンプレート_セクション名)");
        AssertExists<InstanceValueProperty>(properties, "見積書.定型欄.Select(e => e.文)");

        // 見積回答関連
        AssertExists<InstanceValueProperty>(properties, "見積回答.見積書_見積書ID");
        AssertExists<InstanceValueProperty>(properties, "見積回答.見積書_タイトル");
        AssertExists<InstanceValueProperty>(properties, "見積回答.見積書_発行日時");
        AssertExists<InstanceStructureProperty>(properties, "見積回答.見積書_定型欄");
        AssertExists<InstanceValueProperty>(properties, "見積回答.見積書_定型欄.Select(e => e.欄ID)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.見積書_定型欄.Select(e => e.セクションテンプレート_Parent_テンプレートID)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.見積書_定型欄.Select(e => e.セクションテンプレート_Parent_テンプレート名)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.見積書_定型欄.Select(e => e.セクションテンプレート_セクションID)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.見積書_定型欄.Select(e => e.セクションテンプレート_セクション名)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.見積書_定型欄.Select(e => e.文)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.返答日");

        AssertExists<InstanceStructureProperty>(properties, "見積回答.コメント");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.対象_Parent_見積書ID)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.対象_Parent_タイトル)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.対象_Parent_発行日時)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.対象_欄ID)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.対象_セクションテンプレート_セクションID)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.対象_セクションテンプレート_セクション名)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.対象_セクションテンプレート_Parent_テンプレートID)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.対象_セクションテンプレート_Parent_テンプレート名)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.対象_文)");
        AssertExists<InstanceValueProperty>(properties, "見積回答.コメント.Select(e => e.コメント文章)");
    }
}
