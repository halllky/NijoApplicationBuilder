using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;

namespace Nijo.IntegrationTest.Implementors;

public class CommandModelImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "017_CommandModel.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

public partial class OverridedApplicationService {
    public override async Task<CommandModelテストReturnType> Execute(CommandModelテストParameter param, IPresentationContext<CommandModelテストParameterMessages> context) {
        var entity = new テスト集約 {
            テスト集約ID = 1,
            テスト集約名 = param.テスト集約名,
            Version = 1,
        };

        DbContext.テスト集約DbSet.Add(entity);
        await DbContext.SaveChangesAsync();

        return new CommandModelテストReturnType {
            テスト集約ID = entity.テスト集約ID,
        };
    }

    protected override IQueryable<テスト集約SearchResult> CreateQuerySource(テスト集約SearchCondition searchCondition, IPresentationContext<テスト集約Messages> context) {
        return DbContext.テスト集約DbSet.Select(e => new テスト集約SearchResult {
            テスト集約ID = e.テスト集約ID,
            テスト集約名 = e.テスト集約名,
            Version = (int)e.Version!,
        });
    }
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(4));

        // 従業員関連
        AssertExists<InstanceValueProperty>(properties, "従業員.内部ID");
        AssertExists<InstanceValueProperty>(properties, "従業員.従業員コード");
        AssertExists<InstanceValueProperty>(properties, "従業員.名前");
        AssertExists<InstanceValueProperty>(properties, "従業員.区分");
    }
}
