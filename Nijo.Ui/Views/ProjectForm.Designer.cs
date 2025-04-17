namespace Nijo.Ui.Views {
    partial class ProjectForm {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private SplitContainer _splitContainer;
        private Panel _schemaExplorerPanel;
        private TreeView _schemaExplorer;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            _splitContainer = new SplitContainer();
            _schemaExplorerPanel = new Panel();
            _schemaExplorer = new TreeView();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
            _splitContainer.Panel1.SuspendLayout();
            _splitContainer.SuspendLayout();
            _schemaExplorerPanel.SuspendLayout();
            SuspendLayout();
            //
            // _splitContainer
            //
            _splitContainer.Dock = DockStyle.Fill;
            _splitContainer.FixedPanel = FixedPanel.Panel1;
            _splitContainer.Location = new Point(0, 0);
            _splitContainer.Name = "_splitContainer";
            //
            // _splitContainer.Panel1
            //
            _splitContainer.Panel1.Controls.Add(_schemaExplorerPanel);
            _splitContainer.Size = new Size(800, 450);
            _splitContainer.SplitterDistance = 250;
            _splitContainer.TabIndex = 1;
            //
            // _schemaExplorerPanel
            //
            _schemaExplorerPanel.Controls.Add(_schemaExplorer);
            _schemaExplorerPanel.Dock = DockStyle.Fill;
            _schemaExplorerPanel.Location = new Point(0, 0);
            _schemaExplorerPanel.Name = "_schemaExplorerPanel";
            _schemaExplorerPanel.Size = new Size(250, 450);
            _schemaExplorerPanel.TabIndex = 0;
            //
            // _schemaExplorer
            //
            _schemaExplorer.Dock = DockStyle.Fill;
            _schemaExplorer.Location = new Point(0, 0);
            _schemaExplorer.Name = "_schemaExplorer";
            _schemaExplorer.Size = new Size(250, 450);
            _schemaExplorer.TabIndex = 1;
            _schemaExplorer.AfterSelect += SchemaExplorer_AfterSelect;
            //
            // ProjectForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(_splitContainer);
            Name = "ProjectForm";
            Text = "フォルダビュー";
            FormClosing += FolderViewForm_FormClosing;
            _splitContainer.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
            _splitContainer.ResumeLayout(false);
            _schemaExplorerPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
