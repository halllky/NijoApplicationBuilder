using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class ChildOnlyImplementor : IApplicationServiceImplementor {
    public string TargetXmlFileName => "003_Childのみ.xml";

    public string GetImplementation(XDocument schemaXml) {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

public partial class OverridedApplicationService {

    protected override IQueryable<親集約SearchResult> CreateQuerySource(親集約SearchCondition searchCondition, IPresentationContext<親集約Messages> context) {
        return DbContext.親集約DbSet.Select(e => new 親集約SearchResult {
            親集約ID = e.親集約ID,
            親集約名 = e.親集約名,
            子集約_子集約ID = e.子集約!.子集約ID,
            子集約_子集約名 = e.子集約!.子集約名,
            Version = (int)e.Version!,
        });
    }
}";
    }
}
