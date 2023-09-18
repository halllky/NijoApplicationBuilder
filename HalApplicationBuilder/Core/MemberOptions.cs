using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Core {
    public class MemberOptions : IReadOnlyMemberOptions {
        public required string MemberName { get; set; }
        public required IAggregateMemberType MemberType { get; set; }
        public bool IsKey { get; set; }
        public bool IsDisplayName { get; set; }
        public bool IsRequired { get; set; }
        public bool InvisibleInGui { get; set; }
    }

    public interface IReadOnlyMemberOptions {
        string MemberName { get; }
        IAggregateMemberType MemberType { get; }
        bool IsKey { get; }
        bool IsDisplayName { get; }
        bool IsRequired { get; }
        bool InvisibleInGui { get; }
    }

    public static class MemberOptionsExtensions {
        public static IReadOnlyMemberOptions Clone(this IReadOnlyMemberOptions options, Action<MemberOptions> overwrite) {
            var clone = new MemberOptions {
                MemberName = options.MemberName,
                MemberType = options.MemberType,
                IsKey = options.IsKey,
                IsDisplayName = options.IsDisplayName,
                IsRequired = options.IsRequired,
                InvisibleInGui = options.InvisibleInGui,
            };
            overwrite(clone);
            return clone;
        }
    }
}
