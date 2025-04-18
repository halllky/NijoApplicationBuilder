namespace Nijo.Ui.Views {
    partial class AggregateMemberDataGridView {
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
            dataGridView1 = new DataGridView();
            panel1 = new Panel();
            increaseIndentButton = new Button();
            decreaseIndentButton = new Button();
            insertBelowButton = new Button();
            insertRowButton = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            //
            // dataGridView1
            //
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(0, 30);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(150, 120);
            dataGridView1.TabIndex = 0;
            //
            // panel1
            //
            panel1.Controls.Add(increaseIndentButton);
            panel1.Controls.Add(decreaseIndentButton);
            panel1.Controls.Add(insertBelowButton);
            panel1.Controls.Add(insertRowButton);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(150, 30);
            panel1.TabIndex = 1;
            //
            // increaseIndentButton
            //
            increaseIndentButton.Location = new Point(261, 3);
            increaseIndentButton.Name = "increaseIndentButton";
            increaseIndentButton.Size = new Size(100, 23);
            increaseIndentButton.TabIndex = 3;
            increaseIndentButton.Text = "インデントを上げる";
            increaseIndentButton.UseVisualStyleBackColor = true;
            increaseIndentButton.Click += IncreaseIndentButton_Click;
            //
            // decreaseIndentButton
            //
            decreaseIndentButton.Location = new Point(155, 3);
            decreaseIndentButton.Name = "decreaseIndentButton";
            decreaseIndentButton.Size = new Size(100, 23);
            decreaseIndentButton.TabIndex = 2;
            decreaseIndentButton.Text = "インデントを下げる";
            decreaseIndentButton.UseVisualStyleBackColor = true;
            decreaseIndentButton.Click += DecreaseIndentButton_Click;
            //
            // insertBelowButton
            //
            insertBelowButton.Location = new Point(79, 3);
            insertBelowButton.Name = "insertBelowButton";
            insertBelowButton.Size = new Size(70, 23);
            insertBelowButton.TabIndex = 1;
            insertBelowButton.Text = "下挿入";
            insertBelowButton.UseVisualStyleBackColor = true;
            insertBelowButton.Click += InsertBelowButton_Click;
            //
            // insertRowButton
            //
            insertRowButton.Location = new Point(3, 3);
            insertRowButton.Name = "insertRowButton";
            insertRowButton.Size = new Size(70, 23);
            insertRowButton.TabIndex = 0;
            insertRowButton.Text = "行挿入";
            insertRowButton.UseVisualStyleBackColor = true;
            insertRowButton.Click += InsertRowButton_Click;
            //
            // AggregateMemberDataGridView
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(dataGridView1);
            Controls.Add(panel1);
            Name = "AggregateMemberDataGridView";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dataGridView1;
        private Panel panel1;
        private Button insertBelowButton;
        private Button insertRowButton;
        private Button decreaseIndentButton;
        private Button increaseIndentButton;
    }
}
