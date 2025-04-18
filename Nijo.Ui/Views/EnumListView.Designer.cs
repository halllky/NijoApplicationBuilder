namespace Nijo.Ui.Views {
    partial class EnumListView {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._enumsGrid = new System.Windows.Forms.DataGridView();
            this._displayNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._physicalNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._keyColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._panel = new System.Windows.Forms.Panel();
            this._increaseIndentButton = new System.Windows.Forms.Button();
            this._decreaseIndentButton = new System.Windows.Forms.Button();
            this._insertBelowButton = new System.Windows.Forms.Button();
            this._insertRowButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._enumsGrid)).BeginInit();
            this._panel.SuspendLayout();
            this.SuspendLayout();
            //
            // _enumsGrid
            //
            this._enumsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._enumsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._displayNameColumn,
            this._physicalNameColumn,
            this._keyColumn});
            this._enumsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._enumsGrid.Location = new System.Drawing.Point(0, 30);
            this._enumsGrid.Name = "_enumsGrid";
            this._enumsGrid.RowHeadersWidth = 51;
            this._enumsGrid.RowTemplate.Height = 24;
            this._enumsGrid.Size = new System.Drawing.Size(800, 420);
            this._enumsGrid.TabIndex = 0;
            this._enumsGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.EnumsGrid_CellValueChanged);
            //
            // _panel
            //
            this._panel.Controls.Add(this._increaseIndentButton);
            this._panel.Controls.Add(this._decreaseIndentButton);
            this._panel.Controls.Add(this._insertBelowButton);
            this._panel.Controls.Add(this._insertRowButton);
            this._panel.Dock = System.Windows.Forms.DockStyle.Top;
            this._panel.Location = new System.Drawing.Point(0, 0);
            this._panel.Name = "_panel";
            this._panel.Size = new System.Drawing.Size(800, 30);
            this._panel.TabIndex = 1;
            //
            // _insertBelowButton
            //
            this._insertBelowButton.Location = new System.Drawing.Point(79, 3);
            this._insertBelowButton.Name = "_insertBelowButton";
            this._insertBelowButton.Size = new System.Drawing.Size(70, 23);
            this._insertBelowButton.TabIndex = 1;
            this._insertBelowButton.Text = "下挿入";
            this._insertBelowButton.UseVisualStyleBackColor = true;
            this._insertBelowButton.Click += new System.EventHandler(this.InsertBelowButton_Click);
            //
            // _insertRowButton
            //
            this._insertRowButton.Location = new System.Drawing.Point(3, 3);
            this._insertRowButton.Name = "_insertRowButton";
            this._insertRowButton.Size = new System.Drawing.Size(70, 23);
            this._insertRowButton.TabIndex = 0;
            this._insertRowButton.Text = "行挿入";
            this._insertRowButton.UseVisualStyleBackColor = true;
            this._insertRowButton.Click += new System.EventHandler(this.InsertRowButton_Click);
            //
            // _decreaseIndentButton
            //
            this._decreaseIndentButton.Location = new System.Drawing.Point(155, 3);
            this._decreaseIndentButton.Name = "_decreaseIndentButton";
            this._decreaseIndentButton.Size = new System.Drawing.Size(100, 23);
            this._decreaseIndentButton.TabIndex = 2;
            this._decreaseIndentButton.Text = "インデントを下げる";
            this._decreaseIndentButton.UseVisualStyleBackColor = true;
            this._decreaseIndentButton.Click += new System.EventHandler(this.DecreaseIndentButton_Click);
            //
            // _increaseIndentButton
            //
            this._increaseIndentButton.Location = new System.Drawing.Point(261, 3);
            this._increaseIndentButton.Name = "_increaseIndentButton";
            this._increaseIndentButton.Size = new System.Drawing.Size(100, 23);
            this._increaseIndentButton.TabIndex = 3;
            this._increaseIndentButton.Text = "インデントを上げる";
            this._increaseIndentButton.UseVisualStyleBackColor = true;
            this._increaseIndentButton.Click += new System.EventHandler(this.IncreaseIndentButton_Click);
            //
            // _displayNameColumn
            //
            this._displayNameColumn.HeaderText = "表示用名称";
            this._displayNameColumn.MinimumWidth = 6;
            this._displayNameColumn.Name = "_displayNameColumn";
            this._displayNameColumn.Width = 200;
            //
            // _physicalNameColumn
            //
            this._physicalNameColumn.HeaderText = "物理名";
            this._physicalNameColumn.MinimumWidth = 6;
            this._physicalNameColumn.Name = "_physicalNameColumn";
            this._physicalNameColumn.Width = 150;
            //
            // _keyColumn
            //
            this._keyColumn.HeaderText = "キー";
            this._keyColumn.MinimumWidth = 6;
            this._keyColumn.Name = "_keyColumn";
            this._keyColumn.Width = 80;
            //
            // EnumListView
            //
            this.Controls.Add(this._enumsGrid);
            this.Controls.Add(this._panel);
            this.Name = "EnumListView";
            this.Size = new System.Drawing.Size(800, 450);
            ((System.ComponentModel.ISupportInitialize)(this._enumsGrid)).EndInit();
            this._panel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView _enumsGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn _displayNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn _physicalNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn _keyColumn;
        private System.Windows.Forms.Panel _panel;
        private System.Windows.Forms.Button _insertBelowButton;
        private System.Windows.Forms.Button _insertRowButton;
        private System.Windows.Forms.Button _decreaseIndentButton;
        private System.Windows.Forms.Button _increaseIndentButton;
    }
}
