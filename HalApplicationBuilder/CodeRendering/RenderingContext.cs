using System;
using HalApplicationBuilder.Core;

namespace HalApplicationBuilder.CodeRendering
{
    internal class RenderingContext
    {
        internal RenderingContext(ITemplate template, ObjectPath path) {
            Template = template;
            ObjectPath = path;
        }

        internal ITemplate Template { get; }
        internal ObjectPath ObjectPath { get; }

        internal RenderingContext Nest(string obj) {
            return new RenderingContext(Template, ObjectPath.Nest(obj));
        }
    }
}

