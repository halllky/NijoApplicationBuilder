using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nijo {
    /// <summary>
    /// 外部パッケージのインストーラー
    /// </summary>
    internal interface IPackageInstaller {
        Task InstallDependencies(GeneratedProject project, CancellationToken cancellationToken);
    }

    internal class PackageInstaller : IPackageInstaller {
        public async Task InstallDependencies(GeneratedProject project, CancellationToken cancellationToken) {

            await project.ClientDirTerminal
                .Run(new[] { "npm", "ci" }, cancellationToken);

            // dotnetはビルド時に自動的にインストールされるので何もしない
        }
    }
}
