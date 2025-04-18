namespace Nijo.Ui.Views {
    partial class ValueObjectListView {
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
            _valueObjectsGrid = new DataGridView();
            _displayNameColumn = new DataGridViewTextBoxColumn();
            _physicalNameColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)_valueObjectsGrid).BeginInit();
            SuspendLayout();
            // 
            // _valueObjectsGrid
            // 
            _valueObjectsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _valueObjectsGrid.Columns.AddRange(new DataGridViewColumn[] { _displayNameColumn, _physicalNameColumn });
            _valueObjectsGrid.Dock = DockStyle.Fill;
            _valueObjectsGrid.Location = new Point(0, 0);
            _valueObjectsGrid.Name = "_valueObjectsGrid";
            _valueObjectsGrid.Size = new Size(700, 400);
            _valueObjectsGrid.TabIndex = 1;
            _valueObjectsGrid.CellValueChanged += ValueObjectsGrid_CellValueChanged;
            // 
            // _displayNameColumn
            // 
            _displayNameColumn.HeaderText = "表示名";
            _displayNameColumn.Name = "_displayNameColumn";
            _displayNameColumn.Width = 250;
            // 
            // _physicalNameColumn
            // 
            _physicalNameColumn.HeaderText = "物理名";
            _physicalNameColumn.Name = "_physicalNameColumn";
            _physicalNameColumn.ReadOnly = true;
            _physicalNameColumn.Width = 250;
            // 
            // ValueObjectListView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_valueObjectsGrid);
            Name = "ValueObjectListView";
            Size = new Size(700, 400);
            ((System.ComponentModel.ISupportInitialize)_valueObjectsGrid).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private DataGridView _valueObjectsGrid;
        private DataGridViewTextBoxColumn _displayNameColumn;
        private DataGridViewTextBoxColumn _physicalNameColumn;
    }
}
