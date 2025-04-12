using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class MultiplePathRefImplementor : IApplicationServiceImplementor {
    public string TargetXmlFileName => "014_複数経路の参照.xml";

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
}
