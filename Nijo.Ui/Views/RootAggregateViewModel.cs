using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.Ui.Views;

/// <summary>
/// ルート集約1件と対応する。
/// UIをレイアウトに集中させるためにロジックを隠蔽する責務をもつ。
/// </summary>
public class RootAggregateViewModel {

    public RootAggregateViewModel(XElement rootAggregateElement, ProjectFormViewModel projectViewModel) {
        _rootAggregateElement = rootAggregateElement;
        _projectViewModel = projectViewModel;
    }

    private readonly XElement _rootAggregateElement;
    private readonly ProjectFormViewModel _projectViewModel;


}
