namespace Nijo.Ui
{
    partial class ProjectForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private Label _folderPathLabel;
        private SplitContainer _splitContainer;
        private Panel _schemaExplorerPanel;
        private TreeView _schemaExplorer;
        private Label _schemaExplorerLabel;
        private Panel _aggregateDetailPanel;
        private DataGridView _aggregateDetailView;
        private Label _aggregateDetailLabel;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectForm));
            _folderPathLabel = new Label();
            _splitContainer = new SplitContainer();
            _schemaExplorerPanel = new Panel();
            _schemaExplorerLabel = new Label();
            _schemaExplorer = new TreeView();
            _aggregateDetailPanel = new Panel();
            _aggregateDetailLabel = new Label();
            _aggregateDetailView = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
            _splitContainer.Panel1.SuspendLayout();
            _splitContainer.Panel2.SuspendLayout();
            _splitContainer.SuspendLayout();
            _schemaExplorerPanel.SuspendLayout();
            _aggregateDetailPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_aggregateDetailView).BeginInit();
            SuspendLayout();
            //
            // _folderPathLabel
            //
            _folderPathLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _folderPathLabel.AutoEllipsis = true;
            _folderPathLabel.Location = new Point(12, 9);
            _folderPathLabel.Name = "_folderPathLabel";
            _folderPathLabel.Size = new Size(776, 23);
            _folderPathLabel.TabIndex = 0;
            _folderPathLabel.Text = "フォルダパス";
            //
            // _splitContainer
            //
            _splitContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _splitContainer.Location = new Point(12, 35);
            _splitContainer.Name = "_splitContainer";
            //
            // _splitContainer.Panel1
            //
            _splitContainer.Panel1.Controls.Add(_schemaExplorerPanel);
            //
            // _splitContainer.Panel2
            //
            _splitContainer.Panel2.Controls.Add(_aggregateDetailPanel);
            _splitContainer.Size = new Size(776, 403);
            _splitContainer.SplitterDistance = 250;
            _splitContainer.TabIndex = 1;
            //
            // _schemaExplorerPanel
            //
            _schemaExplorerPanel.Controls.Add(_schemaExplorer);
            _schemaExplorerPanel.Controls.Add(_schemaExplorerLabel);
            _schemaExplorerPanel.Dock = DockStyle.Fill;
            _schemaExplorerPanel.Location = new Point(0, 0);
            _schemaExplorerPanel.Name = "_schemaExplorerPanel";
            _schemaExplorerPanel.Size = new Size(250, 403);
            _schemaExplorerPanel.TabIndex = 0;
            //
            // _schemaExplorerLabel
            //
            _schemaExplorerLabel.Dock = DockStyle.Top;
            _schemaExplorerLabel.Location = new Point(0, 0);
            _schemaExplorerLabel.Name = "_schemaExplorerLabel";
            _schemaExplorerLabel.Size = new Size(250, 23);
            _schemaExplorerLabel.TabIndex = 0;
            _schemaExplorerLabel.Text = "スキーマ定義エクスプローラー";
            _schemaExplorerLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // _schemaExplorer
            //
            _schemaExplorer.Dock = DockStyle.Fill;
            _schemaExplorer.Location = new Point(0, 23);
            _schemaExplorer.Name = "_schemaExplorer";
            _schemaExplorer.Size = new Size(250, 380);
            _schemaExplorer.TabIndex = 1;
            _schemaExplorer.AfterSelect += SchemaExplorer_AfterSelect;
            //
            // _aggregateDetailPanel
            //
            _aggregateDetailPanel.Controls.Add(_aggregateDetailView);
            _aggregateDetailPanel.Controls.Add(_aggregateDetailLabel);
            _aggregateDetailPanel.Dock = DockStyle.Fill;
            _aggregateDetailPanel.Location = new Point(0, 0);
            _aggregateDetailPanel.Name = "_aggregateDetailPanel";
            _aggregateDetailPanel.Size = new Size(522, 403);
            _aggregateDetailPanel.TabIndex = 0;
            //
            // _aggregateDetailLabel
            //
            _aggregateDetailLabel.Dock = DockStyle.Top;
            _aggregateDetailLabel.Location = new Point(0, 0);
            _aggregateDetailLabel.Name = "_aggregateDetailLabel";
            _aggregateDetailLabel.Size = new Size(522, 23);
            _aggregateDetailLabel.TabIndex = 0;
            _aggregateDetailLabel.Text = "集約詳細";
            _aggregateDetailLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // _aggregateDetailView
            //
            _aggregateDetailView.AllowUserToAddRows = false;
            _aggregateDetailView.AllowUserToDeleteRows = false;
            _aggregateDetailView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            _aggregateDetailView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _aggregateDetailView.Dock = DockStyle.Fill;
            _aggregateDetailView.Location = new Point(0, 23);
            _aggregateDetailView.Name = "_aggregateDetailView";
            _aggregateDetailView.ReadOnly = true;
            _aggregateDetailView.RowTemplate.Height = 25;
            _aggregateDetailView.Size = new Size(522, 380);
            _aggregateDetailView.TabIndex = 1;
            //
            // ProjectForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(_splitContainer);
            Controls.Add(_folderPathLabel);
            Name = "ProjectForm";
            Text = "フォルダビュー";
            FormClosing += FolderViewForm_FormClosing;
            _splitContainer.Panel1.ResumeLayout(false);
            _splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
            _splitContainer.ResumeLayout(false);
            _schemaExplorerPanel.ResumeLayout(false);
            _aggregateDetailPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_aggregateDetailView).EndInit();
            ResumeLayout(false);
        }

        #endregion
    }
}
