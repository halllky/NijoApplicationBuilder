using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class ChildrenOnlyImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "002_Childrenのみ.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

partial class OverridedApplicationService {
    protected override IQueryable<親集約SearchResult> CreateQuerySource(親集約SearchCondition searchCondition, IPresentationContext<親集約Messages> context) {
        return DbContext.親集約DbSet.Select(e => new 親集約SearchResult {
            親集約ID = e.親集約ID,
            親集約名 = e.親集約名,
        });
    }
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(5));

        AssertExists<InstanceValueProperty>(properties, "親集約.親集約ID");
        AssertExists<InstanceValueProperty>(properties, "親集約.親集約名");
        AssertExists<InstanceStructureProperty>(properties, "親集約.子集約");

        AssertExists<InstanceValueProperty>(properties, "子集約.子集約ID");
        AssertExists<InstanceValueProperty>(properties, "子集約.子集約名");
    }
}
