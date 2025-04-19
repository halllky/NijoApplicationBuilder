using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;

namespace Nijo.IntegrationTest.Implementors;

public class ScalarMemberImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "012_スカラメンバー網羅.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace MyApp.Core;

            partial class OverridedApplicationService {

                protected override IQueryable<親集約SearchResult> CreateQuerySource(親集約SearchCondition searchCondition, IPresentationContext<親集約Messages> context) {
                    return DbContext.親集約DbSet.Select(e => new 親集約SearchResult {
                        ID = e.ID,
                        整数のキー = e.整数のキー,
                        日付のキー = e.日付のキー,
                        列挙体のキー = e.列挙体のキー,
                        値オブジェクトのキー = e.値オブジェクトのキー,
                        単語 = e.単語,
                        単語半角英数 = e.単語半角英数,
                        文章 = e.文章,
                        整数 = e.整数,
                        実数 = e.実数,
                        日付時刻 = e.日付時刻,
                        日付 = e.日付,
                        年月 = e.年月,
                        年 = e.年,
                        参照_参照先_参照先ID = e.参照_参照先ID,
                        参照_参照先_Name = e.参照!.Name,
                        真偽値 = e.真偽値,
                        列挙体 = e.列挙体,
                        Children = e.Children.Select(x => new ChildrenSearchResult {
                            ChildrenId = x.ChildrenId,
                            単語 = x.単語,
                            文章 = x.文章,
                            整数 = x.整数,
                            実数 = x.実数,
                            日付時刻 = x.日付時刻,
                            日付 = x.日付,
                            年月 = x.年月,
                            年 = x.年,
                            参照先ID = x.参照_参照先ID,
                            Name = "TODO: 本来は x.参照.Name と書けるべき",
                            真偽値 = x.真偽値,
                            列挙体 = x.列挙体,
                        }).ToList(),
                        Version = (int)e.Version!,
                    });
                }

                protected override IQueryable<参照先SearchResult> CreateQuerySource(参照先SearchCondition searchCondition, IPresentationContext<参照先Messages> context) {
                    return DbContext.参照先DbSet.Select(e => new 参照先SearchResult {
                        参照先ID = e.参照先ID,
                        Name = e.Name,
                        Version = (int)e.Version!,
                    });
                }
            }
            """;
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(44));

        // 親集約関連
        AssertExists<InstanceValueProperty>(properties, "親集約.ID");
        AssertExists<InstanceValueProperty>(properties, "親集約.整数のキー");
        AssertExists<InstanceValueProperty>(properties, "親集約.日付のキー");
        AssertExists<InstanceValueProperty>(properties, "親集約.列挙体のキー");
        AssertExists<InstanceValueProperty>(properties, "親集約.値オブジェクトのキー");
        AssertExists<InstanceValueProperty>(properties, "親集約.単語");
        AssertExists<InstanceValueProperty>(properties, "親集約.単語半角英数");
        AssertExists<InstanceValueProperty>(properties, "親集約.文章");
        AssertExists<InstanceValueProperty>(properties, "親集約.整数");
        AssertExists<InstanceValueProperty>(properties, "親集約.実数");
        AssertExists<InstanceValueProperty>(properties, "親集約.日付時刻");
        AssertExists<InstanceValueProperty>(properties, "親集約.日付");
        AssertExists<InstanceValueProperty>(properties, "親集約.年月");
        AssertExists<InstanceValueProperty>(properties, "親集約.年");
        AssertExists<InstanceValueProperty>(properties, "親集約.参照_参照先ID");
        AssertExists<InstanceValueProperty>(properties, "親集約.参照_Name");
        AssertExists<InstanceValueProperty>(properties, "親集約.真偽値");
        AssertExists<InstanceValueProperty>(properties, "親集約.列挙体");
        AssertExists<InstanceStructureProperty>(properties, "親集約.Children");

        // Childrenの子要素
        AssertExists<InstanceValueProperty>(properties, "Children.ChildrenId");
        AssertExists<InstanceValueProperty>(properties, "Children.単語");
        AssertExists<InstanceValueProperty>(properties, "Children.文章");
        AssertExists<InstanceValueProperty>(properties, "Children.整数");
        AssertExists<InstanceValueProperty>(properties, "Children.実数");
        AssertExists<InstanceValueProperty>(properties, "Children.日付時刻");
        AssertExists<InstanceValueProperty>(properties, "Children.日付");
        AssertExists<InstanceValueProperty>(properties, "Children.年月");
        AssertExists<InstanceValueProperty>(properties, "Children.年");
        AssertExists<InstanceValueProperty>(properties, "Children.参照_参照先ID");
        AssertExists<InstanceValueProperty>(properties, "Children.参照_Name");
        AssertExists<InstanceValueProperty>(properties, "Children.真偽値");
        AssertExists<InstanceValueProperty>(properties, "Children.列挙体");

        // 参照先関連
        AssertExists<InstanceValueProperty>(properties, "参照先.参照先ID");
        AssertExists<InstanceValueProperty>(properties, "参照先.Name");

        // ユーザーアカウントはQueryModelでないのでSearchResultは生成されない
    }
}
