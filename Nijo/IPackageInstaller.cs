using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

            var npmCi = new Process();
            try {
                npmCi.StartInfo.WorkingDirectory = project.WebClientProjectRoot;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    npmCi.StartInfo.FileName = "powershell";
                    npmCi.StartInfo.Arguments = "/c \"npm ci\"";
                } else {
                    npmCi.StartInfo.FileName = "npm";
                    npmCi.StartInfo.Arguments = "ci";
                }
                npmCi.Start();
                await npmCi.WaitForExitAsync(cancellationToken);

            } finally {
                npmCi.EnsureKill();
            }

            // dotnetはビルド時に自動的にインストールされるので何もしない
        }
    }
}
