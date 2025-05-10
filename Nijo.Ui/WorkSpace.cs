using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;

namespace Nijo.Ui {
    public partial class WorkSpace : Form {
        public WorkSpace() {
            InitializeComponent();
        }

        public WorkSpace(GeneratedProject project) {
            InitializeComponent();
            _project = project;
        }

        private readonly GeneratedProject? _project;
        private WebApplication? _webApplication;

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);

            // Webサイトを開始し、WebView2 にその画面を表示する
            if (_project != null) {
                var nijoUi = new NijoUi(_project);
                var logger = new TestLogger();
                _webApplication = nijoUi.BuildWebApplication(logger);

                var baseUri = new Uri(@"https://localhost:8081");
                _ = _webApplication.RunAsync(baseUri.ToString());

                webView.Source = new Uri(baseUri, "/nijo-ui");
            }
        }

        protected override async void OnFormClosing(FormClosingEventArgs e) {
            base.OnFormClosing(e);

            // Webサイトを停止する
            if (_webApplication != null) {
                try {
                    await _webApplication.StopAsync();
                } catch {
                    // 何もしない
                }
            }
        }
    }
}
