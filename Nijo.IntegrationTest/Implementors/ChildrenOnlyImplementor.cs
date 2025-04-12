using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class ChildrenOnlyImplementor : IApplicationServiceImplementor {
    public string TargetXmlFileName => "children-only.xml";

    public string GetImplementation(XDocument schemaXml) {
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
            住所_市町村 = e.住所!.市町村,
            住所_都道府県 = e.住所!.都道府県,
            Version = (int)e.Version!,
        });
    }
}";
    }
}
