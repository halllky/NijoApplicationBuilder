using System;
using HalApplicationBuilder.ReArchTo関数型.Core;

namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering
{
    internal class RenderingContext
    {
        internal RenderingContext(ITemplate template, ObjectPath path) {
            Template = template;
            ObjectPath = path;
        }

        internal ITemplate Template { get; }
        internal ObjectPath ObjectPath { get; }
    }
}

