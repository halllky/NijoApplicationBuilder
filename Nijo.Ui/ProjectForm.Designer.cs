namespace Nijo.Ui
{
    partial class ProjectForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private Label _folderPathLabel;
        private Panel _contentPanel;

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
            _folderPathLabel = new Label();
            _contentPanel = new Panel();
            SuspendLayout();
            //
            // _folderPathLabel
            //
            _folderPathLabel.AutoSize = false;
            _folderPathLabel.Dock = DockStyle.Top;
            _folderPathLabel.TextAlign = ContentAlignment.MiddleLeft;
            _folderPathLabel.BorderStyle = BorderStyle.Fixed3D;
            _folderPathLabel.Height = 30;
            _folderPathLabel.Padding = new Padding(5, 0, 0, 0);
            _folderPathLabel.Text = "";
            _folderPathLabel.Name = "_folderPathLabel";
            //
            // _contentPanel
            //
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.Location = new Point(0, 30);
            _contentPanel.Name = "_contentPanel";
            _contentPanel.Size = new Size(800, 420);
            _contentPanel.TabIndex = 1;
            //
            // FolderViewForm
            //
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(_contentPanel);
            Controls.Add(_folderPathLabel);
            Name = "FolderViewForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "フォルダビュー";
            FormClosing += FolderViewForm_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
