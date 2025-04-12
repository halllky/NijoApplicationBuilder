using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class ChildrenToChildrenRefImplementor : IApplicationServiceImplementor {
    public string TargetXmlFileName => "010_ChildrenからChildrenへの参照.xml";

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

    protected override IQueryable<親1SearchResult> CreateQuerySource(親1SearchCondition searchCondition, IPresentationContext<親1Messages> context) {
        return DbContext.親1DbSet.Select(e => new 親1SearchResult {
            親1ID = e.親1ID,
            親1名 = e.親1名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<親2SearchResult> CreateQuerySource(親2SearchCondition searchCondition, IPresentationContext<親2Messages> context) {
        return DbContext.親2DbSet.Select(e => new 親2SearchResult {
            親2ID = e.親2ID,
            親2名 = e.親2名,
            Version = (int)e.Version!,
        });
    }
}";
    }
} 