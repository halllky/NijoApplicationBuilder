using System;
using System.Collections.Generic;
using System.Linq;

namespace HalApplicationBuilder.CodeRendering
{
    internal class ObjectPath
    {
        internal ObjectPath(string? rootObjectName = null) {
            _rootObjectName = rootObjectName;
            _path = new List<string>();
        }
        private ObjectPath(string? rootObjectName, IReadOnlyList<string> path) {
            _rootObjectName = rootObjectName;
            _path = path;
        }
        private readonly string? _rootObjectName;
        private readonly IReadOnlyList<string> _path;

        internal int CurrentDepth => _path.Count;

        /// <summary>
        /// ルートオブジェクトからのパス
        /// </summary>
        internal string Path => $"{_rootObjectName}.{string.Join(".", _path)}";
        /// <summary>
        /// ルートオブジェクトからのパス（ルートオブジェクト名を除外したもの）
        /// </summary>
        internal string PathWithoutRoot => string.Join(".", _path);

        internal ObjectPath Nest(string prop) {
            var path = new List<string>(_path);
            path.Add(prop);
            return new ObjectPath(_rootObjectName, path);
        }
    }
}
