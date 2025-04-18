using System;
using System.Windows.Forms;

namespace Nijo.Ui.Views {
    partial class RootAggregateQueryModelView {
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
            this._aggregateDetailPanel = new System.Windows.Forms.Panel();
            this.aggregateMemberDataGridView1 = new Nijo.Ui.Views.AggregateMemberDataGridView();
            this.SuspendLayout();
            //
            // _aggregateDetailPanel
            //
            this._aggregateDetailPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._aggregateDetailPanel.Location = new System.Drawing.Point(0, 0);
            this._aggregateDetailPanel.Name = "_aggregateDetailPanel";
            this._aggregateDetailPanel.Size = new System.Drawing.Size(800, 450);
            this._aggregateDetailPanel.TabIndex = 0;
            //
            // aggregateMemberDataGridView1
            //
            this.aggregateMemberDataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.aggregateMemberDataGridView1.Location = new System.Drawing.Point(0, 0);
            this.aggregateMemberDataGridView1.Name = "aggregateMemberDataGridView1";
            this.aggregateMemberDataGridView1.Size = new System.Drawing.Size(800, 450);
            this.aggregateMemberDataGridView1.TabIndex = 0;
            this.aggregateMemberDataGridView1.Parent = this._aggregateDetailPanel;
            //
            // RootAggregateQueryModelView
            //
            this.Controls.Add(this._aggregateDetailPanel);
            this.Name = "RootAggregateQueryModelView";
            this.Size = new System.Drawing.Size(800, 450);
            this.ResumeLayout(false);
        }

        #endregion

        internal System.Windows.Forms.Panel _aggregateDetailPanel;
        internal Nijo.Ui.Views.AggregateMemberDataGridView aggregateMemberDataGridView1;
    }
}
