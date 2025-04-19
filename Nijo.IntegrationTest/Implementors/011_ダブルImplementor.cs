using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
}
