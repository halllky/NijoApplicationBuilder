using System;
using System.Collections.Generic;
using System.Linq;

namespace HalApplicationBuilder.Runtime {
    /// <summary>
    /// 集約メンバーを一意に特定するための文字列
    /// </summary>
    public class InstanceModelTreePath {
        internal static bool TryParse(string pathString, Core.Aggregate rootAggregate, out InstanceModelTreePath treePath) {
            var paths = (IEnumerable<string>)pathString.Split(".");

            IEnumerable<Core.Aggregate> aggregates = new[] { rootAggregate };
            Core.AggregateMemberBase member = null;
            do {
                var found = aggregates
                    .SelectMany(a => a.Members)
                    .SelectMany(
                        member => member.InstanceModels,
                        (member, instanceMember) => new { member, instanceMember })
                    .SingleOrDefault(a => a.instanceMember.PropertyName == paths.First());

                if (found == null) { treePath = null; return false; }

                member = found.member;
                aggregates = found.member.GetChildAggregates();
                paths = paths.Skip(1);
            } while (paths.Any());

            if (member == null) { treePath = null; return false; }
            treePath = new InstanceModelTreePath { Value = pathString, Member = member };
            return true;
        }

        public string Value { get; private init; }
        public Core.AggregateMemberBase Member { get; private init; }
    }
}
