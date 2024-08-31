using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Core {
    internal class MemberOptions : IReadOnlyMemberOptions {
        public required string MemberName { get; set; }
        public required IAggregateMemberType MemberType { get; set; }
        public bool IsKey { get; set; }
        public bool IsDisplayName { get; set; }
        public bool IsNameLike { get; set; }
        public bool IsRequired { get; set; }
        public bool InvisibleInGui { get; set; }
        public required string? SingleViewCustomUiComponentName { get; set; }
        public required string? SearchConditionCustomUiComponentName { get; set; }
        public required TextBoxWidth? UiWidth { get; set; }
        public required bool WideInVForm { get; set; }
    }

    internal interface IReadOnlyMemberOptions {
        string MemberName { get; }
        IAggregateMemberType MemberType { get; }
        bool IsKey { get; }
        bool IsDisplayName { get; }
        bool IsNameLike { get; }
        bool IsRequired { get; }
        bool InvisibleInGui { get; }
        string? SingleViewCustomUiComponentName { get; }
        string? SearchConditionCustomUiComponentName { get; }
        TextBoxWidth? UiWidth { get; }
        bool WideInVForm { get; }
    }
}
