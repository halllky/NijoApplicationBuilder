using Nijo.Ver1.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Ver1.MutableSchema {

    /// <summary>
    /// <see cref="MutableSchemaNode"/> の集合。
    /// <see cref="ApplicationSchema"/> は不正な状態を許さないが、
    /// こちらはスキーマ編集GUIなどでの利用を想定しているため不正な状態を許す。
    /// </summary>
    internal sealed class MutableSchemaNodeCollection {
    }
}
