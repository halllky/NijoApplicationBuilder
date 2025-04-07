using System.Xml.Linq;

namespace Nijo.IntegrationTest.Implementors;

public class ChildrenOnlyImplementor : IApplicationServiceImplementor
{
    public string TargetXmlFileName => "002_Childrenのみ.xml";

    public string GetImplementation(XDocument schemaXml)
    {
        return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Core;

public partial class OverridedApplicationService {
    public override async Task<QueryModel.親集約一覧> 親集約一覧(QueryModel.親集約一覧.QueryParameter param) {
        var query = DbContext.親集約.AsNoTracking();
        return new QueryModel.親集約一覧 {
            Items = await query
                .Select(x => new QueryModel.親集約一覧.Item {
                    親集約ID = x.親集約ID,
                    親集約名 = x.親集約名,
                    子集約 = x.子集約
                        .Select(y => new QueryModel.親集約一覧.Item.子集約Item {
                            子集約ID = y.子集約ID,
                            子集約名 = y.子集約名,
                        })
                        .ToList(),
                })
                .ToListAsync(),
        };
    }
}";
    }
}
