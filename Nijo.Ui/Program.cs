using Microsoft.Extensions.DependencyInjection;

namespace Nijo.Ui {
    internal static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {

            ApplicationConfiguration.Initialize();

            // サーバーをバックグラウンドで起動
            var cts = new CancellationTokenSource();
            var path = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\自動テストで作成されたプロジェクト";
            Task.Run(async () => {
                var serviceProvider = new ServiceCollection().BuildServiceProvider();
                var project = GeneratedProject.Open(path, serviceProvider);
                var editor = new Nijo.Runtime.NijoUi(project);
                var app = editor.CreateApp();

                await app.RunAsync(WEBAPP_URL);

            }, cts.Token);

            // フォームを起動
            var form = new Form1();
            form.FormClosed += (sender, e) => {
                cts.Cancel();
            };
            Application.Run(form);
        }

        internal const string WEBAPP_URL = "https://localhost:5000";
    }
}
