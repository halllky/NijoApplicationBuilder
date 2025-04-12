using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class NameLikeImplementor : IApplicationServiceImplementor {
    public string TargetXmlFileName => "016_NameLike.xml";

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
        return DbContext.親DbSet
            .Where(e => searchCondition.親名Like == null || e.親名.Contains(searchCondition.親名Like))
            .Select(e => new 親SearchResult {
                親ID = e.親ID,
                親名 = e.親名,
                Version = (int)e.Version!,
            });
    }
}";
    }
}
