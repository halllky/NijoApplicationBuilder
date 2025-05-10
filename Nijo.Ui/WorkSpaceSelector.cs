namespace Nijo.Ui {
    public partial class WorkSpaceSelector : Form {
        public WorkSpaceSelector() {
            InitializeComponent();
        }

        /// <summary>
        /// 動作確認用の暫定
        /// </summary>
        private void button1_Click(object sender, EventArgs e) {

            // nijo.xml があるプロジェクトを指定
            const string APP_DIR = @"C:\Users\krpzx\OneDrive\ドキュメント\local\20230409_haldoc\haldoc\Nijo.ApplicationTemplate.Ver1";
            if (!GeneratedProject.TryOpen(APP_DIR, out var project, out var error)) {
                MessageBox.Show(error);
                return;
            }

            // WorkSpace画面を開く
            using var workSpace = new WorkSpace(project);
            workSpace.ShowDialog();
        }
    }
}
