namespace Nijo.Ui {
    partial class Main {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private Button _openFolderButton;
        private ListBox _recentFoldersListBox;
        private Label _recentFoldersLabel;

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
            _openFolderButton = new Button();
            _recentFoldersListBox = new ListBox();
            _recentFoldersLabel = new Label();
            SuspendLayout();
            // 
            // _openFolderButton
            // 
            _openFolderButton.Location = new Point(6, 6);
            _openFolderButton.Name = "_openFolderButton";
            _openFolderButton.Size = new Size(120, 23);
            _openFolderButton.TabIndex = 0;
            _openFolderButton.Text = "フォルダを開く";
            _openFolderButton.UseVisualStyleBackColor = true;
            _openFolderButton.Click += OpenFolderMenuItem_Click;
            // 
            // _recentFoldersListBox
            // 
            _recentFoldersListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _recentFoldersListBox.FormattingEnabled = true;
            _recentFoldersListBox.Location = new Point(6, 50);
            _recentFoldersListBox.Margin = new Padding(2, 1, 2, 1);
            _recentFoldersListBox.Name = "_recentFoldersListBox";
            _recentFoldersListBox.Size = new Size(420, 154);
            _recentFoldersListBox.TabIndex = 3;
            _recentFoldersListBox.DoubleClick += RecentFoldersListBox_DoubleClick;
            _recentFoldersListBox.KeyDown += RecentFoldersListBox_KeyDown;
            // 
            // _recentFoldersLabel
            // 
            _recentFoldersLabel.AutoSize = true;
            _recentFoldersLabel.Location = new Point(6, 35);
            _recentFoldersLabel.Margin = new Padding(2, 0, 2, 0);
            _recentFoldersLabel.Name = "_recentFoldersLabel";
            _recentFoldersLabel.Size = new Size(100, 15);
            _recentFoldersLabel.TabIndex = 2;
            _recentFoldersLabel.Text = "最近開いたフォルダ:";
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(431, 211);
            Controls.Add(_openFolderButton);
            Controls.Add(_recentFoldersListBox);
            Controls.Add(_recentFoldersLabel);
            Margin = new Padding(2, 1, 2, 1);
            Name = "Main";
            Text = "Nijo";
            Load += Main_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
