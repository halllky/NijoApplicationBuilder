namespace Nijo.Ui
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // UIコンポーネント
        private Label _pathLabel;
        private MenuStrip _menuStrip;
        private ToolStripMenuItem _fileMenu;
        private ToolStripMenuItem _openFolderMenuItem;
        private ToolStripMenuItem _closeFolderMenuItem;

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
            _menuStrip = new MenuStrip();
            _fileMenu = new ToolStripMenuItem();
            _openFolderMenuItem = new ToolStripMenuItem();
            _closeFolderMenuItem = new ToolStripMenuItem();
            _pathLabel = new Label();
            _menuStrip.SuspendLayout();
            SuspendLayout();
            //
            // _menuStrip
            //
            _menuStrip.ImageScalingSize = new Size(32, 32);
            _menuStrip.Items.AddRange(new ToolStripItem[] { _fileMenu });
            _menuStrip.Location = new Point(0, 0);
            _menuStrip.Name = "_menuStrip";
            _menuStrip.Size = new Size(800, 42);
            _menuStrip.TabIndex = 0;
            //
            // _fileMenu
            //
            _fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _openFolderMenuItem, _closeFolderMenuItem });
            _fileMenu.Name = "_fileMenu";
            _fileMenu.Size = new Size(102, 38);
            _fileMenu.Text = "ファイル";
            //
            // _openFolderMenuItem
            //
            _openFolderMenuItem.Name = "_openFolderMenuItem";
            _openFolderMenuItem.Size = new Size(359, 44);
            _openFolderMenuItem.Text = "フォルダを開く";
            _openFolderMenuItem.Click += OpenFolderMenuItem_Click;
            //
            // _closeFolderMenuItem
            //
            _closeFolderMenuItem.Enabled = false;
            _closeFolderMenuItem.Name = "_closeFolderMenuItem";
            _closeFolderMenuItem.Size = new Size(359, 44);
            _closeFolderMenuItem.Text = "フォルダを閉じる";
            _closeFolderMenuItem.Click += CloseFolderMenuItem_Click;
            //
            // _pathLabel
            //
            _pathLabel.AutoSize = false;
            _pathLabel.Dock = DockStyle.Top;
            _pathLabel.TextAlign = ContentAlignment.MiddleLeft;
            _pathLabel.BorderStyle = BorderStyle.Fixed3D;
            _pathLabel.Location = new Point(0, 42);
            _pathLabel.Name = "_pathLabel";
            _pathLabel.Size = new Size(800, 30);
            _pathLabel.Padding = new Padding(5, 0, 0, 0);
            _pathLabel.TabIndex = 1;
            _pathLabel.Text = "フォルダが開かれていません";
            //
            // Main
            //
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(_pathLabel);
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;
            Name = "Main";
            Text = "Nijo";
            _menuStrip.ResumeLayout(false);
            _menuStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
