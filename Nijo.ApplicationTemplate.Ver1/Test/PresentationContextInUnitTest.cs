using MyApp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Test;

/// <summary>
/// <see cref="IPresentationContext"/> のユニットテスト用の実装
/// </summary>
internal class PresentationContextInUnitTest : IPresentationContext {
    internal PresentationContextInUnitTest(Type messageRootType, IPresentationContextOptions options) {
        Messages = MessageContainer.GetDefaultClass(messageRootType, []);
        Options = options;
    }
    protected PresentationContextInUnitTest(IMessageContainer messageRoot, IPresentationContextOptions options) {
        Messages = messageRoot;
        Options = options;
    }

    public IPresentationContextOptions Options { get; }
    public IMessageContainer Messages { get; }
    public List<string> Confirms { get; private set; } = [];

    public void AddConfirm(string text) {
        Confirms.Add(text);
    }
    public bool HasConfirm() {
        return Confirms.Count > 0;
    }

    public IPresentationContext<TMessageRoot> Cast<TMessageRoot>() where TMessageRoot : IMessageContainer {
        return new PresentationContextInUnitTest<TMessageRoot>((TMessageRoot)Messages, Options);
    }

}

/// <inheritdoc cref="PresentationContextInUnitTest"/>
internal class PresentationContextInUnitTest<TMessage> : PresentationContextInUnitTest, IPresentationContext<TMessage> where TMessage : IMessageContainer {
    internal PresentationContextInUnitTest(TMessage messageRoot, IPresentationContextOptions options) : base(messageRoot, options) { }

    public new TMessage Messages => (TMessage)base.Messages;
    IMessageContainer IPresentationContext.Messages => Messages;
}
