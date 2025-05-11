using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace Nijo.Ui {
    public partial class WorkSpaceSelector : Form {
        private Button btnCreateNewWorkspace;
        private TextBox txtWorkspaceName;
        private Label label1;
        private GroupBox groupBox1;
        private Button btnOpenWorkspace;
        private ListView lvRecentWorkspaces;
        private Label lblRecentWorkspaces;
        private RecentWorkspaces _recentWorkspaces;

        public WorkSpaceSelector() {
            InitializeComponent();
            InitializeCustomControls();
            LoadRecentWorkspaces();
        }

        private void InitializeCustomControls() {
            // フォームタイトルの変更
            this.Text = "ワークスペースセレクタ";

            // フォームサイズの調整
            this.ClientSize = new Size(600, 400);

            // ワークスペースを開くボタンの作成
            btnOpenWorkspace = new Button();
            btnOpenWorkspace.Location = new Point(20, 20);
            btnOpenWorkspace.Name = "btnOpenWorkspace";
            btnOpenWorkspace.Size = new Size(200, 30);
            btnOpenWorkspace.TabIndex = 0;
            btnOpenWorkspace.Text = "ワークスペースを開く";
            btnOpenWorkspace.UseVisualStyleBackColor = true;
            btnOpenWorkspace.Click += btnOpenWorkspace_Click;

            // 新しいワークスペース作成用のグループボックス
            groupBox1 = new GroupBox();
            groupBox1.Location = new Point(20, 70);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(560, 100);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "新しいワークスペースを作成";

            // ラベルの作成
            label1 = new Label();
            label1.AutoSize = true;
            label1.Location = new Point(20, 30);
            label1.Name = "label1";
            label1.Size = new Size(124, 15);
            label1.TabIndex = 0;
            label1.Text = "ワークスペース名：";

            // テキストボックスの作成
            txtWorkspaceName = new TextBox();
            txtWorkspaceName.Location = new Point(150, 30);
            txtWorkspaceName.Name = "txtWorkspaceName";
            txtWorkspaceName.Size = new Size(390, 23);
            txtWorkspaceName.TabIndex = 1;

            // ボタンの作成
            btnCreateNewWorkspace = new Button();
            btnCreateNewWorkspace.Location = new Point(340, 60);
            btnCreateNewWorkspace.Name = "btnCreateNewWorkspace";
            btnCreateNewWorkspace.Size = new Size(200, 30);
            btnCreateNewWorkspace.TabIndex = 2;
            btnCreateNewWorkspace.Text = "新しいワークスペースを作成";
            btnCreateNewWorkspace.UseVisualStyleBackColor = true;
            btnCreateNewWorkspace.Click += btnCreateNewWorkspace_Click;

            // 最近のワークスペースラベル
            lblRecentWorkspaces = new Label();
            lblRecentWorkspaces.AutoSize = true;
            lblRecentWorkspaces.Location = new Point(20, 180);
            lblRecentWorkspaces.Name = "lblRecentWorkspaces";
            lblRecentWorkspaces.Size = new Size(200, 15);
            lblRecentWorkspaces.TabIndex = 3;
            lblRecentWorkspaces.Text = "最近開いたワークスペース:";

            // 最近のワークスペースリストビュー
            lvRecentWorkspaces = new ListView();
            lvRecentWorkspaces.Location = new Point(20, 200);
            lvRecentWorkspaces.Name = "lvRecentWorkspaces";
            lvRecentWorkspaces.Size = new Size(560, 180);
            lvRecentWorkspaces.TabIndex = 4;
            lvRecentWorkspaces.UseCompatibleStateImageBehavior = false;
            lvRecentWorkspaces.View = View.Details;
            lvRecentWorkspaces.FullRowSelect = true;
            lvRecentWorkspaces.HideSelection = false;
            lvRecentWorkspaces.Columns.Add("パス", 560);
            lvRecentWorkspaces.DoubleClick += lvRecentWorkspaces_DoubleClick;

            // コントロールを親に追加
            groupBox1.Controls.Add(btnCreateNewWorkspace);
            groupBox1.Controls.Add(txtWorkspaceName);
            groupBox1.Controls.Add(label1);

            // フォームにコントロールを追加
            this.Controls.Add(groupBox1);
            this.Controls.Add(btnOpenWorkspace);
            this.Controls.Add(lblRecentWorkspaces);
            this.Controls.Add(lvRecentWorkspaces);
        }

        /// <summary>
        /// 最近開いたワークスペースを読み込んでリストビューに表示
        /// </summary>
        private void LoadRecentWorkspaces() {
            _recentWorkspaces = new RecentWorkspaces();

            lvRecentWorkspaces.Items.Clear();

            foreach (var workspace in _recentWorkspaces.Workspaces) {
                lvRecentWorkspaces.Items.Add(workspace);
            }
        }

        /// <summary>
        /// 最近開いたワークスペースをリストビューのダブルクリックで開く
        /// </summary>
        private void lvRecentWorkspaces_DoubleClick(object sender, EventArgs e) {
            if (lvRecentWorkspaces.SelectedItems.Count > 0) {
                string projectRoot = lvRecentWorkspaces.SelectedItems[0].Text;
                OpenWorkspace(projectRoot);
            }
        }

        /// <summary>
        /// 指定されたパスのワークスペースを開く
        /// </summary>
        private void OpenWorkspace(string projectRoot) {
            if (GeneratedProject.TryOpen(projectRoot, out var project, out var error)) {
                _recentWorkspaces.AddWorkspace(projectRoot);
                LoadRecentWorkspaces();

                // WorkSpace画面を開く
                using var workSpace = new WorkSpace(project);
                workSpace.ShowDialog();
            } else {
                MessageBox.Show($"ワークスペースを開けませんでした: {error}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 新しいワークスペースを作成する
        /// </summary>
        private void btnCreateNewWorkspace_Click(object sender, EventArgs e) {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "新しいワークスペースの親フォルダを選択してください";

            if (folderDialog.ShowDialog() != DialogResult.OK) {
                return;
            }

            string parentFolder = folderDialog.SelectedPath;
            string workspaceName = txtWorkspaceName.Text.Trim();

            if (string.IsNullOrEmpty(workspaceName)) {
                MessageBox.Show("ワークスペース名を入力してください", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string projectRoot = Path.Combine(parentFolder, workspaceName);

            if (GeneratedProject.TryCreateNewProject(projectRoot, out var project, out var error)) {
                MessageBox.Show($"ワークスペースが作成されました: {projectRoot}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 作成したワークスペースを履歴に追加
                _recentWorkspaces.AddWorkspace(projectRoot);
                LoadRecentWorkspaces();

                // 作成したワークスペースを開く
                using var workSpace = new WorkSpace(project);
                workSpace.ShowDialog();
            } else {
                MessageBox.Show($"ワークスペースの作成に失敗しました: {error}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 既存のワークスペースを開く
        /// </summary>
        private void btnOpenWorkspace_Click(object sender, EventArgs e) {
            using var fileDialog = new OpenFileDialog();
            fileDialog.Title = "nijo.xmlファイルを選択してください";
            fileDialog.Filter = "nijo.xmlファイル|nijo.xml";
            fileDialog.CheckFileExists = true;

            if (fileDialog.ShowDialog() != DialogResult.OK) {
                return;
            }

            string xmlFilePath = fileDialog.FileName;
            string projectRoot = Path.GetDirectoryName(xmlFilePath);

            if (string.IsNullOrEmpty(projectRoot)) {
                MessageBox.Show("有効なプロジェクトフォルダが見つかりませんでした", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenWorkspace(projectRoot);
        }
    }
}
