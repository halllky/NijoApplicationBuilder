namespace Nijo.Ui.Views {
    partial class RootAggregateView {
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
            aggregateMemberDataGridView1 = new AggregateMemberDataGridView();
            _aggregateDetailPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _aggregateDetailPanel
            // 
            _aggregateDetailPanel.BackColor = SystemColors.Control;
            _aggregateDetailPanel.Controls.Add(aggregateMemberDataGridView1);
            _aggregateDetailPanel.Dock = DockStyle.Fill;
            _aggregateDetailPanel.Location = new Point(0, 0);
            _aggregateDetailPanel.Name = "_aggregateDetailPanel";
            _aggregateDetailPanel.Size = new Size(800, 600);
            _aggregateDetailPanel.TabIndex = 0;
            // 
            // aggregateMemberDataGridView1
            // 
            aggregateMemberDataGridView1.Dock = DockStyle.Fill;
            aggregateMemberDataGridView1.ForeColor = SystemColors.ControlText;
            aggregateMemberDataGridView1.Location = new Point(0, 0);
            aggregateMemberDataGridView1.Name = "aggregateMemberDataGridView1";
            aggregateMemberDataGridView1.Size = new Size(800, 600);
            aggregateMemberDataGridView1.TabIndex = 0;
            // 
            // RootAggregateView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlDark;
            Controls.Add(_aggregateDetailPanel);
            Name = "RootAggregateView";
            Size = new Size(800, 600);
            _aggregateDetailPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel _aggregateDetailPanel;
        private AggregateMemberDataGridView aggregateMemberDataGridView1;
    }
}
