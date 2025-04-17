namespace Nijo.Ui {
    partial class ProjectFormDataModelView {
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
            _aggregateDetailPanel = new Panel();
            _aggregateDetailView = new DataGridView();
            _aggregateDetailLabel = new Label();
            _aggregateDetailPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_aggregateDetailView).BeginInit();
            SuspendLayout();
            // 
            // _aggregateDetailPanel
            // 
            _aggregateDetailPanel.Controls.Add(_aggregateDetailView);
            _aggregateDetailPanel.Controls.Add(_aggregateDetailLabel);
            _aggregateDetailPanel.Dock = DockStyle.Fill;
            _aggregateDetailPanel.Location = new Point(0, 0);
            _aggregateDetailPanel.Name = "_aggregateDetailPanel";
            _aggregateDetailPanel.Size = new Size(150, 150);
            _aggregateDetailPanel.TabIndex = 1;
            // 
            // _aggregateDetailView
            // 
            _aggregateDetailView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _aggregateDetailView.Dock = DockStyle.Fill;
            _aggregateDetailView.Location = new Point(0, 23);
            _aggregateDetailView.Name = "_aggregateDetailView";
            _aggregateDetailView.Size = new Size(150, 127);
            _aggregateDetailView.TabIndex = 1;
            // 
            // _aggregateDetailLabel
            // 
            _aggregateDetailLabel.Dock = DockStyle.Top;
            _aggregateDetailLabel.Location = new Point(0, 0);
            _aggregateDetailLabel.Name = "_aggregateDetailLabel";
            _aggregateDetailLabel.Size = new Size(150, 23);
            _aggregateDetailLabel.TabIndex = 0;
            _aggregateDetailLabel.Text = "集約詳細";
            _aggregateDetailLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // ProjectFormDataModelView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_aggregateDetailPanel);
            Name = "ProjectFormDataModelView";
            _aggregateDetailPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_aggregateDetailView).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel _aggregateDetailPanel;
        private DataGridView _aggregateDetailView;
        private Label _aggregateDetailLabel;
    }
}
