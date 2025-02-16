

namespace Nijo.Ui {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e) {
            webView21.Source = new Uri(Program.WEBAPP_URL);
        }
    }
}
