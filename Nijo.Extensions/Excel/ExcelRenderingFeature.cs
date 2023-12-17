using Nijo.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Extensions.Excel {
    internal class ExcelRenderingFeature : NijoFeatureBase {
        public override void EditWebapiProject(NijoCodeGenerator.DirectorySetupper dir) {
            dir.Directory(Util.EXT_DIR, extDir => {
                // TODO
            });
        }

        public override void EditMultiView(IMultiView multiView) {
            multiView.AddUiAction();
        }
    }
}
