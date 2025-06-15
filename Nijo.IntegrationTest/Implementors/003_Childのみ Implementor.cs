using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;

namespace Nijo.IntegrationTest.Implementors;

public class ChildOnlyImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "003_Childのみ.xml";

    public override string GetImplementation(XDocument schemaXml) {
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

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(4));

        AssertExists<InstanceValueProperty>(properties, "親集約.親集約ID");
        AssertExists<InstanceValueProperty>(properties, "親集約.親集約名");
        AssertExists<InstanceValueProperty>(properties, "親集約.子集約_子集約ID");
        AssertExists<InstanceValueProperty>(properties, "親集約.子集約_子集約名");
    }
}
