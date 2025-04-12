using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class CommandModelStepAttributeImplementor : IApplicationServiceImplementor {
    public string TargetXmlFileName => "018_CommandModel_STEP属性つき.xml";

    public string GetImplementation(XDocument schemaXml) {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

public partial class OverridedApplicationService {
    public override async Task<CommandModelテストReturnType> Execute(CommandModelテストParameter param, IPresentationContext<CommandModelテストParameterMessages> context) {
        switch (context.CurrentStep) {
            case ""STEP1"":
                context.Messages.ステップ2入力 = ""ステップ2の入力内容"";
                return await ExecuteStep2(param, context.ChangeStep(""STEP2""));
            case ""STEP2"":
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
            default:
                throw new InvalidOperationException($""不明なステップ: {context.CurrentStep}"");
        }
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
}
