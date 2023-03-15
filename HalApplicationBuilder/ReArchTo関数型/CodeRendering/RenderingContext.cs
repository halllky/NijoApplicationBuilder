using System;
using HalApplicationBuilder.ReArchTo関数型.Core;

namespace HalApplicationBuilder.ReArchTo関数型.CodeRendering
{
    internal class RenderingContext
    {
        internal RenderingContext(ITemplate template) {
            Template = template;
        }

        private readonly string _root;
        internal ITemplate Template { get; }

        internal RenderingContext Nest(AggregateMember member) {
            throw new NotImplementedException();
        }
    }
}

