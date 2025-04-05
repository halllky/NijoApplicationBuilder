using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Test;
internal class PresentationContextInUnitTest<TMessage> : IPresentationContext<TMessage> where TMessage : IMessageContainer {
    internal PresentationContextInUnitTest(TMessage messageRoot, IPresentationContextOptions options) {
        Messages = messageRoot;
        Options = options;
    }

    public TMessage Messages { get; }
    public IPresentationContextOptions Options { get; }

    void IPresentationContext.AddConfirm(string text) {
        throw new NotImplementedException();
    }

    bool IPresentationContext.HasConfirm() {
        throw new NotImplementedException();
    }
}
