using Nijo.CodeGenerating;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class SimpleAggregateImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "000_単純な集約.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

partial class OverridedApplicationService {

    protected override IQueryable<集約ASearchResult> CreateQuerySource(集約ASearchCondition searchCondition, IPresentationContext<集約AMessages> context) {
        return DbContext.集約ADbSet.Select(e => new 集約ASearchResult {
            ID = e.ID,
            名前 = e.名前,
            従属項目 = e.従属項目,
            Version = (int)e.Version!,
        });
    }
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(3));
        AssertExists<InstanceValueProperty>(properties, "集約A.ID");
        AssertExists<InstanceValueProperty>(properties, "集約A.名前");
        AssertExists<InstanceValueProperty>(properties, "集約A.従属項目");
    }
}
