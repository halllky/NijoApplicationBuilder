using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features {
    /// <summary>
    /// TODO: 作りかけ
    /// </summary>
    public abstract class NijoFeatureBase {
        public virtual void EditWebapiProject(NijoCodeGenerator.DirectorySetupper dir) { }
        public virtual void EditReactProject(NijoCodeGenerator.DirectorySetupper dir) { }

        public virtual void EditWebPage(IWebPage webPage) { }
        public virtual void EditMultiView(IMultiView multiView) { }
        public virtual void EditSingleView(ISingleView singleView) { }
    }

    public interface IWebPage {
        void AddMenu(string label, string reactElementName, string? category);
    }
    public interface IMultiView {
        void AddUiAction();
    }
    public interface ISingleView {
        void AddAction();
    }
}
