using Nijo.CodeGenerating.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class RefOnlyImplementor : ApplicationServiceImplementorBase {
    public override string TargetXmlFileName => "001_Refのみ.xml";

    public override string GetImplementation(XDocument schemaXml) {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Core;

partial class OverridedApplicationService {

    protected override IQueryable<参照先SearchResult> CreateQuerySource(参照先SearchCondition searchCondition, IPresentationContext<参照先SearchConditionMessages> context) {
        return DbContext.参照先DbSet.Select(e => new 参照先SearchResult {
            参照先集約ID = e.参照先集約ID,
            参照先集約名 = e.参照先集約名,
            Version = (int)e.Version!,
        });
    }

    protected override IQueryable<参照元SearchResult> CreateQuerySource(参照元SearchCondition searchCondition, IPresentationContext<参照元SearchConditionMessages> context) {
        return DbContext.参照元DbSet.Select(e => new 参照元SearchResult {
            参照元集約ID = e.参照元集約ID,
            参照元集約名 = e.参照元集約名,
            参照_参照先集約ID = e.参照!.参照先集約ID,
            参照_参照先集約名 = e.参照!.参照先集約名,
            Version = (int)e.Version!,
        });
    }
}";
    }

    public override void AssertSearchResultMemberPath(IInstanceProperty[] properties) {
        Assert.That(properties, Has.Length.EqualTo(6));

        AssertExists<InstanceValueProperty>(properties, "参照先.参照先集約ID");
        AssertExists<InstanceValueProperty>(properties, "参照先.参照先集約名");

        AssertExists<InstanceValueProperty>(properties, "参照元.参照元集約ID");
        AssertExists<InstanceValueProperty>(properties, "参照元.参照元集約名");
        AssertExists<InstanceValueProperty>(properties, "参照元.参照_参照先集約ID");
        AssertExists<InstanceValueProperty>(properties, "参照元.参照_参照先集約名");
    }
}
